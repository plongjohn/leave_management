import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';

/** Pages of the app: without a session, sign in first and come back afterwards. */
export const authGuard: CanActivateFn = (_route, state) => {
  if (inject(AuthService).isAuthenticated()) {
    return true;
  }

  return inject(Router).createUrlTree(['/login'], { queryParams: { returnUrl: state.url } });
};

/** The login page: already signed in users go to the app. */
export const guestGuard: CanActivateFn = () => {
  return inject(AuthService).isAuthenticated() ? inject(Router).createUrlTree(['/']) : true;
};

/**
 * Only paths inside the app are followed after login, so a crafted link such as
 * /login?returnUrl=https://evil.example cannot redirect elsewhere (open redirect).
 */
export function safeReturnUrl(returnUrl: string | null | undefined): string {
  return returnUrl?.startsWith('/') && !returnUrl.startsWith('//') && !returnUrl.startsWith('/\\')
    ? returnUrl
    : '/';
}
