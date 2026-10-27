import { Component, inject } from '@angular/core';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatToolbarModule } from '@angular/material/toolbar';
import { RouterOutlet } from '@angular/router';
import { CurrentUserService, MAX_USER_NAME_LENGTH } from './core/services/current-user.service';

@Component({
  imports: [RouterOutlet, MatToolbarModule, MatFormFieldModule, MatInputModule],
  selector: 'app-root',
  styleUrl: './app.scss',
  templateUrl: './app.html',
})
export class App {
  protected readonly currentUser = inject(CurrentUserService);
  protected readonly maxNameLength = MAX_USER_NAME_LENGTH;

  protected onNameInput(event: Event): void {
    this.currentUser.setName((event.target as HTMLInputElement).value);
  }
}
