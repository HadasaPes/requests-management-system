import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { PagedResponse } from '../models/paged-response.model';
import { RequestQuery } from '../models/request-query.model';
import { RequestStats } from '../models/request-stats.model';
import { RequestDto } from '../models/request.model';

/** HTTP calls to the Requests API only: no state, no caching, no error handling. */
@Injectable({ providedIn: 'root' })
export class RequestsApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/api/requests`;

  /** One page of requests. Filtering, search, sorting and paging all run on the server. */
  getRequests(query: RequestQuery): Observable<PagedResponse<RequestDto>> {
    return this.http.get<PagedResponse<RequestDto>>(this.baseUrl, { params: toHttpParams(query) });
  }

  getStats(): Observable<RequestStats> {
    return this.http.get<RequestStats>(`${this.baseUrl}/stats`);
  }
}

/** Empty values are left out, so the API treats them as "no filter". */
function toHttpParams(query: RequestQuery): HttpParams {
  let params = new HttpParams();
  for (const [key, value] of Object.entries(query)) {
    if (value !== null && value !== '') {
      params = params.set(key, value);
    }
  }
  return params;
}
