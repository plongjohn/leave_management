import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { AccessTokenResponse, CurrentUser } from './auth.models';
import { AuthService } from './auth.service';
import { settle } from '../../testing/settle';

describe('AuthService', () => {
  let auth: AuthService;
  let httpTesting: HttpTestingController;

  const sabine: CurrentUser = {
    employeeId: 'e1',
    firstName: 'Sabine',
    lastName: 'Huber',
    email: 'sabine.huber@example.at',
    role: 'Hr',
  };

  const tokenResponse = (accessToken: string): AccessTokenResponse => ({
    accessToken,
    tokenType: 'Bearer',
    expiresIn: 900,
  });

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    auth = TestBed.inject(AuthService);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpTesting.verify());

  it('signs in: sends the credentials, keeps the token and loads the current user', async () => {
    const login = firstValueFrom(
      auth.login({ email: sabine.email, password: 'secret password', rememberMe: true }),
    );

    const loginRequest = httpTesting.expectOne('/api/auth/login');
    expect(loginRequest.request.method).toBe('POST');
    expect(loginRequest.request.body).toEqual({
      email: sabine.email,
      password: 'secret password',
      rememberMe: true,
    });
    loginRequest.flush(tokenResponse('token-1'));
    httpTesting.expectOne('/api/me').flush(sabine);
    await login;

    expect(auth.accessToken()).toBe('token-1');
    expect(auth.currentUser()).toEqual(sabine);
    expect(auth.isAuthenticated()).toBe(true);
  });

  it('shares one refresh request between parallel callers', async () => {
    const first = firstValueFrom(auth.refresh());
    const second = firstValueFrom(auth.refresh());
    await settle();

    httpTesting.expectOne('/api/auth/refresh').flush(tokenResponse('token-2'));

    expect(await first).toBe('token-2');
    expect(await second).toBe('token-2');
    expect(auth.accessToken()).toBe('token-2');
  });

  it('starts a new refresh request after the previous one finished', async () => {
    const first = firstValueFrom(auth.refresh());
    await settle();
    httpTesting.expectOne('/api/auth/refresh').flush(tokenResponse('token-2'));
    await first;

    const second = firstValueFrom(auth.refresh());
    await settle();
    httpTesting.expectOne('/api/auth/refresh').flush(tokenResponse('token-3'));

    expect(await second).toBe('token-3');
  });

  it('restores the session with the refresh cookie', async () => {
    const restore = auth.restoreSession();
    await settle();

    httpTesting.expectOne('/api/auth/refresh').flush(tokenResponse('token-4'));
    await settle();
    httpTesting.expectOne('/api/me').flush(sabine);
    await restore;

    expect(auth.currentUser()).toEqual(sabine);
  });

  it('stays signed out when the refresh cookie is missing or invalid', async () => {
    const restore = auth.restoreSession();
    await settle();

    httpTesting
      .expectOne('/api/auth/refresh')
      .flush(null, { status: 401, statusText: 'Unauthorized' });
    await restore;

    expect(auth.isAuthenticated()).toBe(false);
    expect(auth.accessToken()).toBeNull();
  });

  it('signs out locally even if the logout request fails', async () => {
    await signIn();

    const logout = firstValueFrom(auth.logout(), { defaultValue: undefined });
    httpTesting
      .expectOne('/api/auth/logout')
      .flush(null, { status: 500, statusText: 'Server Error' });
    await logout;

    expect(auth.isAuthenticated()).toBe(false);
    expect(auth.accessToken()).toBeNull();
  });

  async function signIn(): Promise<void> {
    const login = firstValueFrom(
      auth.login({ email: sabine.email, password: 'secret password', rememberMe: false }),
    );
    httpTesting.expectOne('/api/auth/login').flush(tokenResponse('token-1'));
    httpTesting.expectOne('/api/me').flush(sabine);
    await login;
  }
});
