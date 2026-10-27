import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, TestRequest, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MatSnackBar } from '@angular/material/snack-bar';
import { By } from '@angular/platform-browser';
import { environment } from '../../../../environments/environment';
import { PagedResponse } from '../../../core/models/paged-response.model';
import { RequestStats } from '../../../core/models/request-stats.model';
import { RequestDto } from '../../../core/models/request.model';
import { CurrentUserService } from '../../../core/services/current-user.service';
import { RequestsTable } from '../components/requests-table/requests-table';
import { RequestsPage } from './requests-page';

const BASE_URL = `${environment.apiUrl}/api/requests`;
const SEARCH_DEBOUNCE_MS = 300;

const EMPTY_STATS: RequestStats = {
  totalCount: 0,
  countByStatus: { New: 0, InProgress: 0, Waiting: 0, Completed: 0 },
  countByPriority: { Low: 0, Medium: 0, High: 0 },
};

function makeRequest(id: number, overrides: Partial<RequestDto> = {}): RequestDto {
  return {
    id,
    title: `Request ${id}`,
    organizationName: 'Acme',
    status: 'New',
    priority: 'Medium',
    assignedTo: null,
    createdAt: '2026-01-01T00:00:00Z',
    updatedAt: '2026-01-01T00:00:00Z',
    rowVersion: `rv-${id}`,
    allowedNextStatuses: ['InProgress'],
    ...overrides,
  };
}

function pageOf(items: RequestDto[]): PagedResponse<RequestDto> {
  return { items, page: 1, pageSize: 20, totalCount: items.length, totalPages: 1 };
}

describe('RequestsPage', () => {
  let fixture: ComponentFixture<RequestsPage>;
  let httpTesting: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [RequestsPage],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTesting.verify();
    vi.useRealTimers();
  });

  /** Renders the page and answers the initial stats and list calls. */
  function createPage(initialItems: RequestDto[] = []): void {
    fixture = TestBed.createComponent(RequestsPage);
    fixture.detectChanges();
    httpTesting.expectOne(`${BASE_URL}/stats`).flush(EMPTY_STATS);
    const initial = expectListRequest();
    expect(initial.request.params.has('search')).toBe(false);
    initial.flush(pageOf(initialItems));
    fixture.detectChanges();
  }

  function expectListRequest(): TestRequest {
    return httpTesting.expectOne((r) => r.method === 'GET' && r.url === BASE_URL);
  }

  function pendingListRequests(): TestRequest[] {
    return httpTesting.match((r) => r.method === 'GET' && r.url === BASE_URL);
  }

  function typeSearch(text: string): void {
    const input = fixture.nativeElement.querySelector('app-requests-filters input[type="search"]') as HTMLInputElement;
    input.value = text;
    input.dispatchEvent(new Event('input'));
  }

  function table(): RequestsTable {
    return fixture.debugElement.query(By.directive(RequestsTable)).componentInstance as RequestsTable;
  }

  describe('search stream', () => {
    beforeEach(() => vi.useFakeTimers());

    it('sends only one GET for several quick keystrokes, after the debounce time', () => {
      createPage();

      typeSearch('r');
      vi.advanceTimersByTime(100);
      typeSearch('re');
      vi.advanceTimersByTime(100);
      typeSearch('req');

      vi.advanceTimersByTime(SEARCH_DEBOUNCE_MS - 1);
      expect(pendingListRequests()).toHaveLength(0);

      vi.advanceTimersByTime(1);
      const requests = pendingListRequests();
      expect(requests).toHaveLength(1);
      expect(requests[0].request.params.get('search')).toBe('req');
      expect(requests[0].request.params.get('page')).toBe('1');

      requests[0].flush(pageOf([]));
    });

    it('cancels a pending GET when the search changes and shows only the newest result', () => {
      createPage([makeRequest(1)]);

      typeSearch('old');
      vi.advanceTimersByTime(SEARCH_DEBOUNCE_MS);
      const oldRequest = expectListRequest();
      expect(oldRequest.request.params.get('search')).toBe('old');

      typeSearch('new');
      vi.advanceTimersByTime(SEARCH_DEBOUNCE_MS);
      const newRequest = expectListRequest();
      expect(newRequest.request.params.get('search')).toBe('new');

      // switchMap unsubscribed from the old call, which cancels the HTTP request.
      expect(oldRequest.cancelled).toBe(true);
      expect(newRequest.cancelled).toBe(false);

      const newest = makeRequest(2, { title: 'Newest result' });
      newRequest.flush(pageOf([newest]));
      fixture.detectChanges();

      expect(table().items()).toEqual([newest]);
      const text = (fixture.nativeElement as HTMLElement).textContent;
      expect(text).toContain('Newest result');
      expect(text).not.toContain('Request 1');
    });
  });

  describe('successful status update', () => {
    it('replaces the row at once, then reloads the current page with the same query and reloads the stats', () => {
      vi.spyOn(TestBed.inject(MatSnackBar), 'open').mockReturnValue(undefined as never);
      TestBed.inject(CurrentUserService).setName('Dana');

      fixture = TestBed.createComponent(RequestsPage);
      fixture.detectChanges();
      httpTesting.expectOne(`${BASE_URL}/stats`).flush(EMPTY_STATS);
      const initial = expectListRequest();
      const initialParams = initial.request.params;
      const row1 = makeRequest(1);
      initial.flush(pageOf([row1, makeRequest(2)]));
      fixture.detectChanges();

      table().statusChange.emit({ request: row1, status: 'InProgress' });

      const updated = makeRequest(1, { status: 'InProgress', rowVersion: 'rv-1-new' });
      httpTesting.expectOne(`${BASE_URL}/1/status`).flush(updated);
      fixture.detectChanges();

      // Shown at once, before the reload returns.
      expect(table().items()[0]).toEqual(updated);

      const reload = expectListRequest();
      expect(reload.request.params.keys().sort()).toEqual(initialParams.keys().sort());
      for (const key of initialParams.keys()) {
        expect(reload.request.params.get(key)).toBe(initialParams.get(key));
      }
      httpTesting.expectOne(`${BASE_URL}/stats`).flush(EMPTY_STATS);

      // The list now shows the server's page (for example, row 1 left it because it no longer matches a filter).
      reload.flush({ ...pageOf([makeRequest(2)]), totalCount: 1 });
      fixture.detectChanges();
      expect(table().items().map((r) => r.id)).toEqual([2]);
    });
  });

  describe('409 handling', () => {
    it('loads only the changed row, replaces it in the table and shows the conflict message', () => {
      const snackBarOpen = vi.spyOn(TestBed.inject(MatSnackBar), 'open').mockReturnValue(undefined as never);
      TestBed.inject(CurrentUserService).setName('Dana');

      const row1 = makeRequest(1);
      const row2 = makeRequest(2);
      createPage([row1, row2]);

      table().statusChange.emit({ request: row1, status: 'InProgress' });

      const patch = httpTesting.expectOne(`${BASE_URL}/1/status`);
      expect(patch.request.method).toBe('PATCH');
      expect(patch.request.body).toEqual({ status: 'InProgress', rowVersion: 'rv-1', changedBy: 'Dana' });
      patch.flush({ title: 'Conflict', status: 409 }, { status: 409, statusText: 'Conflict' });

      const reload = httpTesting.expectOne(`${BASE_URL}/1`);
      expect(reload.request.method).toBe('GET');
      const latest = makeRequest(1, {
        title: 'Changed by someone else',
        status: 'Waiting',
        rowVersion: 'rv-1-new',
        allowedNextStatuses: ['InProgress', 'Completed'],
      });
      reload.flush(latest);
      fixture.detectChanges();

      // Only row 1 is replaced; row 2 is the same object, and the list is not reloaded (verify() in afterEach).
      const items = table().items();
      expect(items).toHaveLength(2);
      expect(items[0]).toEqual(latest);
      expect(items[1]).toBe(row2);
      expect((fixture.nativeElement as HTMLElement).textContent).toContain('Changed by someone else');

      expect(snackBarOpen).toHaveBeenCalledWith(
        'This request was changed by someone else. The latest version was loaded. Please try again.',
        'OK',
        { duration: 8000 },
      );
    });
  });
});
