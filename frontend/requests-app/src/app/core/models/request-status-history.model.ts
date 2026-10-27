import { RequestStatus } from './request.model';

/** Matches RequestsBL.Dtos.RequestStatusHistoryDto. */
export interface RequestStatusHistoryDto {
  id: number;
  previousStatus: RequestStatus;
  newStatus: RequestStatus;
  /** UTC ISO string. */
  changedAt: string;
  changedBy: string;
}
