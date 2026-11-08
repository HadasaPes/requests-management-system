import { Injectable, computed, signal } from '@angular/core';

export const MAX_USER_NAME_LENGTH = 100;

@Injectable({ providedIn: 'root' })
export class CurrentUserService {
  private readonly nameSignal = signal('');

  readonly name = this.nameSignal.asReadonly();
  readonly hasName = computed(() => this.nameSignal().length > 0);

  setName(name: string): void {
    this.nameSignal.set(name.trim().slice(0, MAX_USER_NAME_LENGTH));
  }
}
