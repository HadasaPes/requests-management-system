export const REQUEST_STATUSES = ['New', 'InProgress', 'Waiting', 'Completed'] as const;
export type RequestStatus = (typeof REQUEST_STATUSES)[number];

export const REQUEST_PRIORITIES = ['Low', 'Medium', 'High'] as const;
export type RequestPriority = (typeof REQUEST_PRIORITIES)[number];

export interface RequestDto {
  id: number;
  title: string;
  organizationName: string;
  status: RequestStatus;
  priority: RequestPriority;
  assignedTo: string | null;
  createdAt: string;
  updatedAt: string;
  rowVersion: string;
  allowedNextStatuses: RequestStatus[];
}
