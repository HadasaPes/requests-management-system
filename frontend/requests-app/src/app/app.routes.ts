import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    title: 'Requests',
    loadComponent: () => import('./features/requests/requests-page/requests-page').then((m) => m.RequestsPage),
  },
  { path: '**', redirectTo: '' },
];
