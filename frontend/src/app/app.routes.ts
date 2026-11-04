import { Routes } from '@angular/router';
import { authGuard, guestGuard } from './core/auth/auth.guards';
import { loginTexts } from './features/login/login.texts';
import { overviewTexts } from './features/my-requests/my-requests.texts';

export const routes: Routes = [
  {
    path: 'login',
    title: loginTexts.pageTitle,
    canActivate: [guestGuard],
    loadComponent: () => import('./features/login/login-page/login-page').then((m) => m.LoginPage),
  },
  {
    // Everything else needs a session (the API denies by default, too).
    path: '',
    canActivate: [authGuard],
    loadComponent: () => import('./core/layout/app-shell/app-shell').then((m) => m.AppShell),
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'overview' },
      {
        path: 'overview',
        title: overviewTexts.pageTitle,
        loadComponent: () =>
          import('./features/my-requests/overview-page/overview-page').then((m) => m.OverviewPage),
      },
      {
        path: 'employees',
        loadComponent: () =>
          import('./features/employees/employee-list/employee-list').then((m) => m.EmployeeList),
      },
    ],
  },
  { path: '**', redirectTo: '' },
];
