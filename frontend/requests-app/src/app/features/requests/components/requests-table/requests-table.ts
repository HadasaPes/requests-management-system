import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatSortModule, Sort } from '@angular/material/sort';
import { MatTableModule } from '@angular/material/table';
import { PAGE_SIZE_OPTIONS, RequestSort, RequestSortField, SortDirection } from '../../../../core/models/request-query.model';
import { RequestDto } from '../../../../core/models/request.model';

export interface PageChange {
  pageIndex: number;
  pageSize: number;
}

/** Shows one page of requests. Sorting and paging are only reported; the server does the work. */
@Component({
  selector: 'app-requests-table',
  imports: [DatePipe, MatTableModule, MatSortModule, MatPaginatorModule],
  templateUrl: './requests-table.html',
  styleUrl: './requests-table.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RequestsTable {
  readonly items = input.required<RequestDto[]>();
  readonly totalCount = input.required<number>();
  /** 1-based, like the API. */
  readonly page = input.required<number>();
  readonly pageSize = input.required<number>();
  readonly sort = input.required<RequestSort>();

  readonly sortChange = output<RequestSort>();
  readonly pageChange = output<PageChange>();

  protected readonly columns = ['id', 'title', 'organizationName', 'status', 'priority', 'assignedTo', 'createdAt', 'updatedAt'];
  protected readonly pageSizeOptions = PAGE_SIZE_OPTIONS;

  // matSortDisableClear in the template: a column toggles asc/desc and never goes back to "no sort".
  protected onSort(sort: Sort): void {
    this.sortChange.emit({ sortBy: sort.active as RequestSortField, sortDirection: sort.direction as SortDirection });
  }

  protected onPage(event: PageEvent): void {
    this.pageChange.emit({ pageIndex: event.pageIndex, pageSize: event.pageSize });
  }
}
