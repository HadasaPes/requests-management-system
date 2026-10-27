import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { PagedResponse } from '../models/paged-response.model';
import { RequestQuery } from '../models/request-query.model';
import { RequestStats } from '../models/request-stats.model';
import { RequestStatusHistoryDto } from '../models/request-status-history.model';
import { RequestDto } from '../models/request.model';
import { UpdateStatusRequest } from '../models/update-status-request.model';

/** HTTP calls to the Requests API only: no state, no caching, no error handling. */
@Injectable({ providedIn: 'root' })
export class RequestsApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/api/requests`;

  /** One page of requests. Filtering, search, sorting and paging all run on the server. */
  getRequests(query: RequestQuery): Observable<PagedResponse<RequestDto>> {
    return this.http.get<PagedResponse<RequestDto>>(this.baseUrl, { params: toHttpParams(query) });
  }

  getRequest(id: number): Observable<RequestDto> {
    return this.http.get<RequestDto>(`${this.baseUrl}/${id}`);
  }

  getStats(): Observable<RequestStats> {
    return this.http.get<RequestStats>(`${this.baseUrl}/stats`);
  }

  /** Returns the updated request with its new rowVersion; 409 if the rowVersion is no longer current. */
  updateStatus(id: number, body: UpdateStatusRequest): Observable<RequestDto> {
    return this.http.patch<RequestDto>(`${this.baseUrl}/${id}/status`, body);
  }

  /** Newest first. */
  getHistory(id: number): Observable<RequestStatusHistoryDto[]> {
    return this.http.get<RequestStatusHistoryDto[]>(`${this.baseUrl}/${id}/history`);
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
