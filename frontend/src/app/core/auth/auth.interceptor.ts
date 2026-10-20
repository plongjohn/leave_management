import { HttpErrorResponse, HttpInterceptorFn, HttpRequest } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, switchMap, throwError } from 'rxjs';
import { AuthService } from './auth.service';

/**
 * Sends the access token with every API request. On 401 (token expired) it refreshes once and
 * repeats the request; if the refresh fails, the session is over and the user signs in again.
 * The auth endpoints themselves work with the refresh cookie and are left alone.
 */
export const authInterceptor: HttpInterceptorFn = (request, next) => {
  if (!request.url.startsWith('/api/') || request.url.startsWith('/api/auth/')) {
    return next(request);
  }

  const auth = inject(AuthService);
  const router = inject(Router);

  return next(withToken(request, auth.accessToken())).pipe(
    catchError((error: unknown) => {
      if (!(error instanceof HttpErrorResponse) || error.status !== 401) {
        return throwError(() => error);
      }

      return auth.refresh().pipe(
        catchError((refreshError: unknown) => {
          auth.clearSession();
          void router.navigate(['/login'], { queryParams: { returnUrl: router.url } });
          return throwError(() => refreshError);
        }),
        switchMap((accessToken) => next(withToken(request, accessToken))),
      );
    }),
  );
};

function withToken<T>(request: HttpRequest<T>, accessToken: string | null): HttpRequest<T> {
  return accessToken
    ? request.clone({ setHeaders: { Authorization: `Bearer ${accessToken}` } })
    : request;
}
