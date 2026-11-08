import { RequestStatus } from './request.model';

export interface UpdateStatusRequest {
  status: RequestStatus;
  rowVersion: string;
  changedBy: string;
}
