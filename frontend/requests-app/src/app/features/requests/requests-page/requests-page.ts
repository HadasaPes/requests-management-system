import { AsyncPipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSnackBar } from '@angular/material/snack-bar';
import {
  BehaviorSubject,
  Observable,
  Subject,
  catchError,
  combineLatest,
  debounceTime,
  distinctUntilChanged,
  finalize,
  map,
  merge,
  of,
  scan,
  startWith,
  switchMap,
  throwError,
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
import { CurrentUserService } from '../../../core/services/current-user.service';
import { RequestsApiService } from '../../../core/services/requests-api.service';
import { ErrorMessage, toErrorMessage } from '../../../core/utils/error-message';
import { RequestHistoryDialog, RequestHistoryDialogData } from '../components/request-history-dialog/request-history-dialog';
import { RequestsFilters } from '../components/requests-filters/requests-filters';
import { RequestsStats } from '../components/requests-stats/requests-stats';
import { PageChange, RequestsTable, StatusChange } from '../components/requests-table/requests-table';

interface ListState {
  query: RequestQuery | null;
  loading: boolean;
  result: PagedResponse<RequestDto> | null;
  error: ErrorMessage | null;
}

interface StatsState {
  stats: RequestStats | null;
  failed: boolean;
}

type ListUpdate = (state: ListState) => ListState;

const INITIAL_LIST_STATE: ListState = { query: null, loading: true, result: null, error: null };

const CONFLICT_MESSAGE =
  'This request was changed by someone else. The latest version was loaded. Please try again.';

@Component({
  selector: 'app-requests-page',
  imports: [AsyncPipe, MatButtonModule, MatProgressBarModule, RequestsFilters, RequestsTable, RequestsStats],
  templateUrl: './requests-page.html',
  styleUrl: './requests-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RequestsPage {
  private readonly api = inject(RequestsApiService);
  private readonly snackBar = inject(MatSnackBar);
  private readonly dialog = inject(MatDialog);
  private readonly destroyRef = inject(DestroyRef);
  protected readonly currentUser = inject(CurrentUserService);

  private readonly searchText$ = new Subject<string>();
  private readonly filters$ = new BehaviorSubject<RequestFilters>(EMPTY_FILTERS);
  private readonly sort$ = new BehaviorSubject<RequestSort>(DEFAULT_SORT);
  private readonly pageSize$ = new BehaviorSubject<number>(DEFAULT_PAGE_SIZE);
  private readonly pageIndex$ = new Subject<number>();
  private readonly reload$ = new Subject<void>();
  private readonly rowReplaced$ = new Subject<RequestDto>();
  private readonly statsReload$ = new Subject<void>();

  protected readonly savingIds = signal<ReadonlySet<number>>(new Set());

  private readonly search$ = this.searchText$.pipe(
    debounceTime(300),
    map((text) => text.trim()),
    startWith(''),
    distinctUntilChanged(),
  );

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

  private readonly loads$: Observable<ListUpdate> = combineLatest([this.query$, this.reload$.pipe(startWith(undefined))]).pipe(
    switchMap(([query]) =>
      this.api.getRequests(query).pipe(
        map((result): Partial<ListState> => ({ loading: false, result, error: null })),
        catchError((error: unknown) => of<Partial<ListState>>({ loading: false, result: null, error: toErrorMessage(error) })),
        startWith<Partial<ListState>>({ query, loading: true, error: null }),
      ),
    ),
    map((change) => (state) => ({ ...state, ...change })),
  );

  private readonly rowReplacements$: Observable<ListUpdate> = this.rowReplaced$.pipe(
    map((updated) => (state) =>
      state.result
        ? { ...state, result: { ...state.result, items: state.result.items.map((r) => (r.id === updated.id ? updated : r)) } }
        : state,
    ),
  );

  protected readonly list$: Observable<ListState> = merge(this.loads$, this.rowReplacements$).pipe(
    scan((state, update) => update(state), INITIAL_LIST_STATE),
  );

  protected readonly stats$: Observable<StatsState> = this.statsReload$.pipe(
    startWith(undefined),
    switchMap(() =>
      this.api.getStats().pipe(
        map((stats) => ({ stats, failed: false })),
        catchError(() => of({ stats: null, failed: true })),
      ),
    ),
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
      this.pageSize$.next(pageSize);
    } else {
      this.pageIndex$.next(pageIndex);
    }
  }

  protected retry(): void {
    this.reload$.next();
  }

  protected onStatusChange({ request, status }: StatusChange): void {
    const changedBy = this.currentUser.name();
    if (!changedBy || this.savingIds().has(request.id)) {
      return;
    }

    this.setSaving(request.id, true);
    this.api
      .updateStatus(request.id, { status, rowVersion: request.rowVersion, changedBy })
      .pipe(
        map((updated) => ({ updated, conflict: false })),
        catchError((error: unknown) =>
          isStatus(error, 409)
            ? this.api.getRequest(request.id).pipe(map((latest) => ({ updated: latest, conflict: true })))
            : throwError(() => error),
        ),
        finalize(() => this.setSaving(request.id, false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: ({ updated, conflict }) => {
          this.rowReplaced$.next(updated);
          if (conflict) {
            this.snackBar.open(CONFLICT_MESSAGE, 'OK', { duration: 8000 });
          } else {
            this.snackBar.open(`Request #${updated.id} is now ${updated.status}.`, undefined, { duration: 3000 });
            this.reload$.next();
            this.statsReload$.next();
          }
        },
        error: (error: unknown) => {
          const { message, details } = toErrorMessage(error);
          this.snackBar.open(details.length ? details.join(' ') : message, 'OK', { duration: 8000 });
          if (isStatus(error, 404)) {
            this.reload$.next();
          }
        },
      });
  }

  protected openHistory(request: RequestDto): void {
    this.dialog.open<RequestHistoryDialog, RequestHistoryDialogData>(RequestHistoryDialog, {
      data: { request },
      width: '520px',
      maxWidth: '95vw',
    });
  }

  private setSaving(id: number, saving: boolean): void {
    this.savingIds.update((ids) => {
      const next = new Set(ids);
      if (saving) {
        next.add(id);
      } else {
        next.delete(id);
      }
      return next;
    });
  }
}

function isStatus(error: unknown, status: number): boolean {
  return error instanceof HttpErrorResponse && error.status === status;
}
