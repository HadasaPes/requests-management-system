import { AsyncPipe, DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { Observable, Subject, catchError, map, of, startWith, switchMap } from 'rxjs';
import { RequestStatusHistoryDto } from '../../../../core/models/request-status-history.model';
import { RequestDto } from '../../../../core/models/request.model';
import { RequestsApiService } from '../../../../core/services/requests-api.service';
import { ErrorMessage, toErrorMessage } from '../../../../core/utils/error-message';

export interface RequestHistoryDialogData {
  request: RequestDto;
}

type HistoryState =
  | { kind: 'loading' }
  | { kind: 'loaded'; entries: RequestStatusHistoryDto[] }
  | { kind: 'error'; error: ErrorMessage };

/** Status change history of one request, newest first. Opened with MatDialog; loads its own data. */
@Component({
  selector: 'app-request-history-dialog',
  imports: [AsyncPipe, DatePipe, MatDialogModule, MatButtonModule, MatProgressBarModule],
  templateUrl: './request-history-dialog.html',
  styleUrl: './request-history-dialog.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RequestHistoryDialog {
  private readonly api = inject(RequestsApiService);
  protected readonly request = inject<RequestHistoryDialogData>(MAT_DIALOG_DATA).request;

  private readonly retry$ = new Subject<void>();

  // The async pipe subscribes when the dialog opens and unsubscribes when it closes (cancelling a pending call).
  protected readonly state$: Observable<HistoryState> = this.retry$.pipe(
    startWith(undefined),
    switchMap(() =>
      this.api.getHistory(this.request.id).pipe(
        map((entries): HistoryState => ({ kind: 'loaded', entries })),
        catchError((error: unknown) => of<HistoryState>({ kind: 'error', error: toErrorMessage(error) })),
        startWith<HistoryState>({ kind: 'loading' }),
      ),
    ),
  );

  protected retry(): void {
    this.retry$.next();
  }
}
