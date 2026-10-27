export const REQUEST_STATUSES = ['New', 'InProgress', 'Waiting', 'Completed'] as const;
export type RequestStatus = (typeof REQUEST_STATUSES)[number];

export const REQUEST_PRIORITIES = ['Low', 'Medium', 'High'] as const;
export type RequestPriority = (typeof REQUEST_PRIORITIES)[number];

/** Matches RequestsBL.Dtos.RequestDto. Dates are UTC ISO strings. */
export interface RequestDto {
  id: number;
  title: string;
  organizationName: string;
  status: RequestStatus;
  priority: RequestPriority;
  assignedTo: string | null;
  createdAt: string;
  updatedAt: string;
  /** Base64; sent back when changing the status (optimistic concurrency). */
  rowVersion: string;
  /** The statuses this request can be changed to next, from the API's transition rules (empty when Completed). */
  allowedNextStatuses: RequestStatus[];
}
