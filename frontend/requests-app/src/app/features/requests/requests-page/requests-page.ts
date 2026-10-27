import { AsyncPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import {
  BehaviorSubject,
  Observable,
  Subject,
  catchError,
  combineLatest,
  debounceTime,
  distinctUntilChanged,
  map,
  of,
  scan,
  startWith,
  switchMap,
} from 'rxjs';
import { PagedResponse } from '../../../core/models/paged-response.model';
import {
  DEFAULT_PAGE_SIZE,
  DEFAULT_SORT,
  EMPTY_FILTERS,
  RequestFilters,
  RequestQuery,
  RequestSort,
} from '../../../core/models/request-query.model';
import { RequestStats } from '../../../core/models/request-stats.model';
import { RequestDto } from '../../../core/models/request.model';
import { RequestsApiService } from '../../../core/services/requests-api.service';
import { ErrorMessage, toErrorMessage } from '../../../core/utils/error-message';
import { RequestsFilters } from '../components/requests-filters/requests-filters';
import { RequestsStats } from '../components/requests-stats/requests-stats';
import { PageChange, RequestsTable } from '../components/requests-table/requests-table';

interface ListState {
  query: RequestQuery | null;
  loading: boolean;
  /** The last loaded page; kept while the next one loads, so the table does not jump. */
  result: PagedResponse<RequestDto> | null;
  error: ErrorMessage | null;
}

interface StatsState {
  stats: RequestStats | null;
  failed: boolean;
}

const INITIAL_LIST_STATE: ListState = { query: null, loading: true, result: null, error: null };

/** Owns the list state: turns user input into API calls and the responses into what the view shows. */
@Component({
  selector: 'app-requests-page',
  imports: [AsyncPipe, MatButtonModule, MatProgressBarModule, RequestsFilters, RequestsTable, RequestsStats],
  templateUrl: './requests-page.html',
  styleUrl: './requests-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RequestsPage {
  private readonly api = inject(RequestsApiService);

  // User input. Each one is a stream; the list below is computed from all of them.
  private readonly searchText$ = new Subject<string>();
  private readonly filters$ = new BehaviorSubject<RequestFilters>(EMPTY_FILTERS);
  private readonly sort$ = new BehaviorSubject<RequestSort>(DEFAULT_SORT);
  private readonly pageSize$ = new BehaviorSubject<number>(DEFAULT_PAGE_SIZE);
  private readonly pageIndex$ = new Subject<number>();
  private readonly retry$ = new Subject<void>();

  /** Only the search text is debounced; the first (empty) value is used at once so the list loads immediately. */
  private readonly search$ = this.searchText$.pipe(
    debounceTime(300),
    map((text) => text.trim()),
    startWith(''),
    distinctUntilChanged(),
  );

  /**
   * The query to load. Any change of search, filters, sort or page size starts a new inner stream at page 1;
   * page changes only move within it. So "changing a filter resets to page 1" needs no extra code.
   */
  private readonly query$: Observable<RequestQuery> = combineLatest({
    search: this.search$,
    filters: this.filters$,
    sort: this.sort$,
    pageSize: this.pageSize$,
  }).pipe(
    switchMap(({ search, filters, sort, pageSize }) =>
      this.pageIndex$.pipe(
        startWith(0),
        map((pageIndex): RequestQuery => ({ ...filters, ...sort, search, pageSize, page: pageIndex + 1 })),
      ),
    ),
  );

  /**
   * One request at a time: switchMap unsubscribes from the previous call when a new query arrives,
   * which cancels the HTTP request, so an old, slower response can never overwrite a newer one.
   */
  protected readonly list$: Observable<ListState> = combineLatest([this.query$, this.retry$.pipe(startWith(undefined))]).pipe(
    switchMap(([query]) =>
      this.api.getRequests(query).pipe(
        map((result): Partial<ListState> => ({ loading: false, result, error: null })),
        catchError((error: unknown) => of<Partial<ListState>>({ loading: false, result: null, error: toErrorMessage(error) })),
        startWith<Partial<ListState>>({ query, loading: true, error: null }),
      ),
    ),
    scan((state, change) => ({ ...state, ...change }), INITIAL_LIST_STATE),
  );

  protected readonly stats$: Observable<StatsState> = this.api.getStats().pipe(
    map((stats) => ({ stats, failed: false })),
    catchError(() => of({ stats: null, failed: true })),
  );

  protected onSearchChange(text: string): void {
    this.searchText$.next(text);
  }

  protected onFiltersChange(filters: RequestFilters): void {
    this.filters$.next(filters);
  }

  protected onSortChange(sort: RequestSort): void {
    this.sort$.next(sort);
  }

  protected onPageChange({ pageIndex, pageSize }: PageChange): void {
    if (pageSize !== this.pageSize$.value) {
      this.pageSize$.next(pageSize); // back to page 1
    } else {
      this.pageIndex$.next(pageIndex);
    }
  }

  protected retry(): void {
    this.retry$.next();
  }
}
