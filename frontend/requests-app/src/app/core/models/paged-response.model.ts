/** Matches RequestsBL.Dtos.PagedResponse<T>. */
export interface PagedResponse<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}
