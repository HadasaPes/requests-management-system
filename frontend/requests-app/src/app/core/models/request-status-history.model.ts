import { RequestStatus } from './request.model';

export interface RequestStatusHistoryDto {
  id: number;
  previousStatus: RequestStatus;
  newStatus: RequestStatus;
  changedAt: string;
  changedBy: string;
}
