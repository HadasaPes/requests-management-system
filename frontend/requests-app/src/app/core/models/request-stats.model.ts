import { RequestPriority, RequestStatus } from './request.model';

export interface RequestStats {
  totalCount: number;
  countByStatus: Record<RequestStatus, number>;
  countByPriority: Record<RequestPriority, number>;
}
