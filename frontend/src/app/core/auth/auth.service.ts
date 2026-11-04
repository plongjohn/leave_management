import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import {
  Observable,
  catchError,
  defer,
  finalize,
  firstValueFrom,
  map,
  of,
  shareReplay,
  switchMap,
  tap,
} from 'rxjs';
import { AccessTokenResponse, CurrentUser, LoginCredentials } from './auth.models';
import { withRefreshLock } from './refresh-lock';

/**
 * Holds the session (ADR 0004): the access token lives in memory only, the refresh token in an
 * HttpOnly cookie the app never sees. After a page reload, restoreSession() gets a new access
 * token with that cookie.
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);

  private readonly token = signal<string | null>(null);
  private readonly user = signal<CurrentUser | null>(null);
  private refreshInFlight: Observable<string> | null = null;

  readonly currentUser = this.user.asReadonly();
  readonly isAuthenticated = computed(() => this.user() !== null);

  accessToken(): string | null {
    return this.token();
  }

  login(credentials: LoginCredentials): Observable<void> {
    return this.http.post<AccessTokenResponse>('/api/auth/login', credentials).pipe(
      tap((response) => this.token.set(response.accessToken)),
      switchMap(() => this.loadCurrentUser()),
    );
  }

  /** Ends the session on the server; the local session ends even if that request fails. */
  logout(): Observable<void> {
    return this.http.post<void>('/api/auth/logout', null).pipe(
      catchError(() => of(undefined)),
      map(() => undefined),
      finalize(() => this.clearSession()),
    );
  }

  /**
   * Gets a new access token with the refresh cookie. Parallel callers share one request,
   * because every refresh rotates the cookie.
   */
  refresh(): Observable<string> {
    this.refreshInFlight ??= defer(() =>
      withRefreshLock(() =>
        firstValueFrom(this.http.post<AccessTokenResponse>('/api/auth/refresh', null)),
      ),
    ).pipe(
      map((response) => response.accessToken),
      tap((accessToken) => this.token.set(accessToken)),
      finalize(() => (this.refreshInFlight = null)),
      shareReplay({ bufferSize: 1, refCount: false }),
    );

    return this.refreshInFlight;
  }

  /** Called once at start-up; without a valid refresh cookie the user simply stays signed out. */
  async restoreSession(): Promise<void> {
    try {
      await firstValueFrom(this.refresh().pipe(switchMap(() => this.loadCurrentUser())));
    } catch {
      this.clearSession();
    }
  }

  clearSession(): void {
    this.token.set(null);
    this.user.set(null);
  }

  private loadCurrentUser(): Observable<void> {
    return this.http.get<CurrentUser>('/api/me').pipe(
      tap((user) => this.user.set(user)),
      map(() => undefined),
    );
  }
}
