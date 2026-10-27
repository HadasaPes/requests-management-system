import { RequestStatus } from './request.model';

/** Matches RequestsBL.Dtos.UpdateStatusRequestDto (body of PATCH /api/requests/{id}/status). */
export interface UpdateStatusRequest {
  status: RequestStatus;
  rowVersion: string;
  changedBy: string;
}
