import { Injectable, computed, signal } from '@angular/core';

/** Max length of changedBy in the API. */
export const MAX_USER_NAME_LENGTH = 100;

/**
 * The name sent as changedBy. Kept in memory only (lost on reload).
 * There is no authentication, so the API cannot verify it (see docs/known-limitations.md).
 */
@Injectable({ providedIn: 'root' })
export class CurrentUserService {
  private readonly nameSignal = signal('');

  readonly name = this.nameSignal.asReadonly();
  readonly hasName = computed(() => this.nameSignal().length > 0);

  setName(name: string): void {
    this.nameSignal.set(name.trim().slice(0, MAX_USER_NAME_LENGTH));
  }
}
