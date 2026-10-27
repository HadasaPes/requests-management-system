import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../../environments/environment';
import { RequestQuery } from '../models/request-query.model';
import { RequestsApiService } from './requests-api.service';

describe('RequestsApiService', () => {
  let service: RequestsApiService;
  let httpTesting: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(RequestsApiService);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpTesting.verify());

  it('getRequests sends the query as URL parameters and leaves out empty values', () => {
    const query: RequestQuery = {
      search: '',
      status: 'InProgress',
      priority: null,
      organizationName: 'Acme',
      assignedTo: null,
      createdFrom: '2026-01-01',
      createdTo: null,
      sortBy: 'title',
      sortDirection: 'asc',
      page: 2,
      pageSize: 50,
    };

    service.getRequests(query).subscribe();

    const req = httpTesting.expectOne((r) => r.url === `${environment.apiUrl}/api/requests`);
    expect(req.request.method).toBe('GET');

    const params = req.request.params;
    expect(params.keys().sort()).toEqual(
      ['createdFrom', 'organizationName', 'page', 'pageSize', 'sortBy', 'sortDirection', 'status'],
    );
    expect(params.get('status')).toBe('InProgress');
    expect(params.get('organizationName')).toBe('Acme');
    expect(params.get('createdFrom')).toBe('2026-01-01');
    expect(params.get('sortBy')).toBe('title');
    expect(params.get('sortDirection')).toBe('asc');
    expect(params.get('page')).toBe('2');
    expect(params.get('pageSize')).toBe('50');

    req.flush({ items: [], page: 2, pageSize: 50, totalCount: 0, totalPages: 0 });
  });
});
