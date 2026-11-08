import { ChangeDetectionStrategy, Component } from '@angular/core';
import { outputFromObservable } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { distinctUntilChanged, map } from 'rxjs';
import { RequestFilters } from '../../../../core/models/request-query.model';
import { REQUEST_PRIORITIES, REQUEST_STATUSES, RequestPriority, RequestStatus } from '../../../../core/models/request.model';

@Component({
  selector: 'app-requests-filters',
  imports: [ReactiveFormsModule, MatFormFieldModule, MatInputModule, MatSelectModule, MatButtonModule],
  templateUrl: './requests-filters.html',
  styleUrl: './requests-filters.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RequestsFilters {
  protected readonly statuses = REQUEST_STATUSES;
  protected readonly priorities = REQUEST_PRIORITIES;

  protected readonly search = new FormControl('', { nonNullable: true });

  protected readonly form = new FormGroup({
    status: new FormControl<RequestStatus | null>(null),
    priority: new FormControl<RequestPriority | null>(null),
    organizationName: new FormControl<string | null>(null, { updateOn: 'blur' }),
    assignedTo: new FormControl<string | null>(null, { updateOn: 'blur' }),
    createdFrom: new FormControl<string | null>(null),
    createdTo: new FormControl<string | null>(null),
  });

  readonly searchChange = outputFromObservable(this.search.valueChanges);

  readonly filtersChange = outputFromObservable(
    this.form.valueChanges.pipe(
      map(() => toFilters(this.form.getRawValue())),
      distinctUntilChanged((a, b) => JSON.stringify(a) === JSON.stringify(b)),
    ),
  );

  protected apply(event: Event): void {
    (event.target as HTMLInputElement).blur();
  }

  protected clear(): void {
    this.search.setValue('');
    this.form.reset();
  }
}

function toFilters(value: RequestFilters): RequestFilters {
  return {
    ...value,
    organizationName: value.organizationName?.trim() || null,
    assignedTo: value.assignedTo?.trim() || null,
    createdFrom: value.createdFrom || null,
    createdTo: value.createdTo || null,
  };
}
