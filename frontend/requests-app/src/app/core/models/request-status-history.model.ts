import { RequestStatus } from './request.model';

export interface RequestStatusHistoryDto {
  id: number;
  previousStatus: RequestStatus;
  newStatus: RequestStatus;
  // UTC ISO string.
  changedAt: string;
  changedBy: string;
}
