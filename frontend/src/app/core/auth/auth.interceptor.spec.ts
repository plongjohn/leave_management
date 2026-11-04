import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { authInterceptor } from './auth.interceptor';
import { AuthService } from './auth.service';
import { settle } from '../../testing/settle';

describe('authInterceptor', () => {
  let http: HttpClient;
  let httpTesting: HttpTestingController;
  let auth: AuthService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
      ],
    });
    http = TestBed.inject(HttpClient);
    httpTesting = TestBed.inject(HttpTestingController);
    auth = TestBed.inject(AuthService);
  });

  afterEach(() => httpTesting.verify());

  async function giveToken(accessToken: string): Promise<void> {
    const refresh = firstValueFrom(auth.refresh());
    await settle();
    httpTesting
      .expectOne('/api/auth/refresh')
      .flush({ accessToken, tokenType: 'Bearer', expiresIn: 900 });
    await refresh;
  }

  it('sends the access token with API requests', async () => {
    await giveToken('token-1');

    const response = firstValueFrom(http.get('/api/employees'));
    const request = httpTesting.expectOne('/api/employees');
    request.flush([]);
    await response;

    expect(request.request.headers.get('Authorization')).toBe('Bearer token-1');
  });

  it('does not send the access token to the auth endpoints', async () => {
    await giveToken('token-1');

    const response = firstValueFrom(http.post('/api/auth/logout', null));
    const request = httpTesting.expectOne('/api/auth/logout');
    request.flush(null);
    await response;

    expect(request.request.headers.has('Authorization')).toBe(false);
  });

  it('refreshes once on 401 and repeats the request with the new token', async () => {
    await giveToken('expired-token');

    const response = firstValueFrom(http.get<string[]>('/api/employees'));
    httpTesting
      .expectOne('/api/employees')
      .flush(null, { status: 401, statusText: 'Unauthorized' });
    await settle();
    httpTesting
      .expectOne('/api/auth/refresh')
      .flush({ accessToken: 'token-2', tokenType: 'Bearer', expiresIn: 900 });
    await settle();
    const retry = httpTesting.expectOne('/api/employees');
    retry.flush(['Anna']);

    expect(await response).toEqual(['Anna']);
    expect(retry.request.headers.get('Authorization')).toBe('Bearer token-2');
  });

  it('ends the session and opens the login page when the refresh fails', async () => {
    await giveToken('expired-token');
    const router = TestBed.inject(Router);
    const navigate = vi.spyOn(router, 'navigate').mockResolvedValue(true);

    const response = firstValueFrom(http.get('/api/employees'));
    httpTesting
      .expectOne('/api/employees')
      .flush(null, { status: 401, statusText: 'Unauthorized' });
    await settle();
    httpTesting
      .expectOne('/api/auth/refresh')
      .flush(null, { status: 401, statusText: 'Unauthorized' });

    await expect(response).rejects.toBeDefined();
    expect(auth.accessToken()).toBeNull();
    expect(navigate).toHaveBeenCalledWith(['/login'], { queryParams: { returnUrl: '/' } });
  });

  it('passes other errors through without refreshing', async () => {
    await giveToken('token-1');

    const response = firstValueFrom(http.get('/api/employees'));
    httpTesting
      .expectOne('/api/employees')
      .flush(null, { status: 500, statusText: 'Server Error' });

    await expect(response).rejects.toMatchObject({ status: 500 });
  });
});
