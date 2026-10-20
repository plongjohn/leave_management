import { Routes } from '@angular/router';
import { guestGuard } from './core/auth/auth.guards';
import { loginTexts } from './features/login/login.texts';

export const routes: Routes = [
  {
    path: 'login',
    title: loginTexts.pageTitle,
    canActivate: [guestGuard],
    loadComponent: () => import('./features/login/login-page/login-page').then((m) => m.LoginPage),
  },
  { path: '', pathMatch: 'full', redirectTo: 'employees' },
  {
    path: 'employees',
    loadComponent: () =>
      import('./features/employees/employee-list/employee-list').then((m) => m.EmployeeList),
  },
];
