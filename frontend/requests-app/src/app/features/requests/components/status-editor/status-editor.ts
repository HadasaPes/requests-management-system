import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { MatMenuModule } from '@angular/material/menu';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { RequestStatus } from '../../../../core/models/request.model';

/** The current status as a menu button; the menu lists only the statuses the API allows next. */
@Component({
  selector: 'app-status-editor',
  imports: [MatMenuModule, MatProgressSpinnerModule],
  templateUrl: './status-editor.html',
  styleUrl: './status-editor.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StatusEditor {
  readonly status = input.required<RequestStatus>();
  readonly allowedNext = input.required<RequestStatus[]>();
  /** False until the user entered a name. */
  readonly canEdit = input.required<boolean>();
  /** True while this row's update is in flight, to prevent a double submit. */
  readonly saving = input(false);

  /** Only reports the choice; the page sends the update. */
  readonly statusSelected = output<RequestStatus>();

  protected readonly disabled = computed(() => this.saving() || !this.canEdit() || this.allowedNext().length === 0);

  protected readonly hint = computed(() => {
    if (this.saving()) return 'Saving…';
    if (this.allowedNext().length === 0) return `${this.status()} is a final status`;
    if (!this.canEdit()) return 'Enter your name at the top to change the status';
    return 'Change status';
  });
}
