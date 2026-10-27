import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatSortModule, Sort } from '@angular/material/sort';
import { MatTableModule } from '@angular/material/table';
import { PAGE_SIZE_OPTIONS, RequestSort, RequestSortField, SortDirection } from '../../../../core/models/request-query.model';
import { RequestDto, RequestStatus } from '../../../../core/models/request.model';
import { StatusEditor } from '../status-editor/status-editor';

export interface PageChange {
  pageIndex: number;
  pageSize: number;
}

export interface StatusChange {
  request: RequestDto;
  status: RequestStatus;
}

/** Shows one page of requests. Sorting, paging, status changes and history are only reported; the page does the work. */
@Component({
  selector: 'app-requests-table',
  imports: [DatePipe, MatTableModule, MatSortModule, MatPaginatorModule, MatButtonModule, StatusEditor],
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
  /** False until the user entered a name. */
  readonly canEdit = input.required<boolean>();
  /** Ids of the rows whose status update is in flight. */
  readonly savingIds = input.required<ReadonlySet<number>>();

  readonly sortChange = output<RequestSort>();
  readonly pageChange = output<PageChange>();
  readonly statusChange = output<StatusChange>();
  readonly historyOpen = output<RequestDto>();

  protected readonly columns = [
    'id', 'title', 'organizationName', 'status', 'priority', 'assignedTo', 'createdAt', 'updatedAt', 'actions',
  ];
  protected readonly pageSizeOptions = PAGE_SIZE_OPTIONS;

  // matSortDisableClear in the template: a column toggles asc/desc and never goes back to "no sort".
  protected onSort(sort: Sort): void {
    this.sortChange.emit({ sortBy: sort.active as RequestSortField, sortDirection: sort.direction as SortDirection });
  }

  protected onPage(event: PageEvent): void {
    this.pageChange.emit({ pageIndex: event.pageIndex, pageSize: event.pageSize });
  }
}
