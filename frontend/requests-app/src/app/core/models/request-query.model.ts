import { RequestPriority, RequestStatus } from './request.model';

export type RequestSortField = 'createdAt' | 'updatedAt' | 'priority' | 'status' | 'title' | 'organizationName';
export type SortDirection = 'asc' | 'desc';

export interface RequestSort {
  sortBy: RequestSortField;
  sortDirection: SortDirection;
}

export interface RequestFilters {
  status: RequestStatus | null;
  priority: RequestPriority | null;
  organizationName: string | null;
  assignedTo: string | null;
  createdFrom: string | null;
  createdTo: string | null;
}

export interface RequestQuery extends RequestFilters, RequestSort {
  search: string;
  page: number;
  pageSize: number;
}

export const EMPTY_FILTERS: RequestFilters = {
  status: null,
  priority: null,
  organizationName: null,
  assignedTo: null,
  createdFrom: null,
  createdTo: null,
};

export const DEFAULT_SORT: RequestSort = { sortBy: 'createdAt', sortDirection: 'desc' };

export const DEFAULT_PAGE_SIZE = 20;

export const PAGE_SIZE_OPTIONS = [10, 20, 50, 100];
