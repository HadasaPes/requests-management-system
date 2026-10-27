import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { RequestStats } from '../../../../core/models/request-stats.model';
import { REQUEST_PRIORITIES, REQUEST_STATUSES } from '../../../../core/models/request.model';

/** Dashboard summary of all requests (not affected by the list filters). */
@Component({
  selector: 'app-requests-stats',
  imports: [DecimalPipe],
  templateUrl: './requests-stats.html',
  styleUrl: './requests-stats.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RequestsStats {
  /** Null while loading or if loading failed. */
  readonly stats = input.required<RequestStats | null>();
  readonly failed = input(false);

  protected readonly statuses = REQUEST_STATUSES;
  protected readonly priorities = REQUEST_PRIORITIES;
}
