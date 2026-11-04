import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import {
  ActivatedRouteSnapshot,
  Router,
  RouterStateSnapshot,
  UrlTree,
  provideRouter,
} from '@angular/router';
import { authGuard, guestGuard, safeReturnUrl } from './auth.guards';
import { AuthService } from './auth.service';

describe('auth guards', () => {
  const isAuthenticated = signal(false);

  beforeEach(() => {
    isAuthenticated.set(false);
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: { isAuthenticated: isAuthenticated.asReadonly() } },
      ],
    });
  });

  function runAuthGuard(url: string): ReturnType<typeof authGuard> {
    return TestBed.runInInjectionContext(() =>
      authGuard({} as ActivatedRouteSnapshot, { url } as RouterStateSnapshot),
    );
  }

  function runGuestGuard(): ReturnType<typeof guestGuard> {
    return TestBed.runInInjectionContext(() =>
      guestGuard({} as ActivatedRouteSnapshot, { url: '/login' } as RouterStateSnapshot),
    );
  }

  function serialize(result: unknown): string {
    return TestBed.inject(Router).serializeUrl(result as UrlTree);
  }

  it('sends signed-out users to the login page and remembers where they wanted to go', () => {
    expect(serialize(runAuthGuard('/employees'))).toBe('/login?returnUrl=%2Femployees');
  });

  it('lets signed-in users into the app', () => {
    isAuthenticated.set(true);

    expect(runAuthGuard('/employees')).toBe(true);
  });

  it('shows the login page to signed-out users', () => {
    expect(runGuestGuard()).toBe(true);
  });

  it('sends signed-in users from the login page to the app', () => {
    isAuthenticated.set(true);

    expect(serialize(runGuestGuard())).toBe('/');
  });

  it.each([
    ['/employees?page=2', '/employees?page=2'],
    ['https://evil.example', '/'],
    ['//evil.example', '/'],
    ['/\\evil.example', '/'],
    [null, '/'],
  ])('only follows return URLs inside the app (%s)', (returnUrl, expected) => {
    expect(safeReturnUrl(returnUrl)).toBe(expected);
  });
});
