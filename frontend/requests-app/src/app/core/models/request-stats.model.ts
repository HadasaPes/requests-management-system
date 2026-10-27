import { RequestPriority, RequestStatus } from './request.model';

/** Matches RequestsBL.Dtos.RequestStatsDto. Every status and priority is present, with 0 if there are none. */
export interface RequestStats {
  totalCount: number;
  countByStatus: Record<RequestStatus, number>;
  countByPriority: Record<RequestPriority, number>;
}
