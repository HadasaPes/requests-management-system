import { Injectable, computed, signal } from '@angular/core';

// Must match the API's changedBy limit.
export const MAX_USER_NAME_LENGTH = 100;

// In memory only; there is no authentication, so the API cannot verify the name.
@Injectable({ providedIn: 'root' })
export class CurrentUserService {
  private readonly nameSignal = signal('');

  readonly name = this.nameSignal.asReadonly();
  readonly hasName = computed(() => this.nameSignal().length > 0);

  setName(name: string): void {
    this.nameSignal.set(name.trim().slice(0, MAX_USER_NAME_LENGTH));
  }
}
