import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    title: 'TaskBoard',
    loadComponent: () =>
      import('./features/dashboard/pages/dashboard-page/dashboard-page').then(
        (m) => m.DashboardPage,
      ),
  },
  { path: '**', redirectTo: '' },
];
