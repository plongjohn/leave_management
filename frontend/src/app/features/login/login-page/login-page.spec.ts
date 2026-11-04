import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router, convertToParamMap, provideRouter } from '@angular/router';
import { Observable, Subject, of, throwError } from 'rxjs';
import { LoginCredentials } from '../../../core/auth/auth.models';
import { AuthService } from '../../../core/auth/auth.service';
import { settle } from '../../../testing/settle';
import { LoginPage } from './login-page';

describe('LoginPage', () => {
  let login: ReturnType<typeof vi.fn<(credentials: LoginCredentials) => Observable<void>>>;
  let navigateByUrl: ReturnType<typeof vi.spyOn>;

  async function render(returnUrl?: string): Promise<HTMLElement> {
    login = vi.fn<(credentials: LoginCredentials) => Observable<void>>(() => of(undefined));

    TestBed.configureTestingModule({
      imports: [LoginPage],
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: { login } },
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: { queryParamMap: convertToParamMap(returnUrl ? { returnUrl } : {}) },
          },
        },
      ],
    });
    navigateByUrl = vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);

    const fixture = TestBed.createComponent(LoginPage);
    await fixture.whenStable();
    return fixture.nativeElement as HTMLElement;
  }

  function fill(element: HTMLElement, email: string, password: string): void {
    setValue(element.querySelector<HTMLInputElement>('input[type="email"]')!, email);
    setValue(
      element.querySelector<HTMLInputElement>('input[autocomplete="current-password"]')!,
      password,
    );
  }

  function setValue(input: HTMLInputElement, value: string): void {
    input.value = value;
    input.dispatchEvent(new Event('input'));
  }

  async function submit(element: HTMLElement): Promise<void> {
    element.querySelector<HTMLButtonElement>('button[type="submit"]')!.click();
    await settle();
  }

  it('does not send an empty form and explains what is missing', async () => {
    const element = await render();

    await submit(element);

    expect(login).not.toHaveBeenCalled();
    expect(element.textContent).toContain('Bitte gib deine E-Mail-Adresse ein.');
    expect(element.textContent).toContain('Bitte gib dein Passwort ein.');
  });

  it('signs in with trimmed email and the remember-me choice, then opens the return URL', async () => {
    const element = await render('/employees');
    fill(element, ' sabine.huber@example.at ', 'secret password');
    element.querySelector<HTMLInputElement>('input[type="checkbox"]')!.click();

    await submit(element);

    expect(login).toHaveBeenCalledWith({
      email: 'sabine.huber@example.at',
      password: 'secret password',
      rememberMe: true,
    });
    expect(navigateByUrl).toHaveBeenCalledWith('/employees');
  });

  it('ignores return URLs outside the app', async () => {
    const element = await render('https://evil.example');
    fill(element, 'sabine.huber@example.at', 'secret password');

    await submit(element);

    expect(navigateByUrl).toHaveBeenCalledWith('/');
  });

  it.each([
    [401, 'E-Mail oder Passwort ist falsch, oder das Konto ist vorübergehend gesperrt.'],
    [429, 'Zu viele Anmeldeversuche.'],
    [0, 'Die Anmeldung ist gerade nicht möglich.'],
  ])('shows a message when the login fails with status %s', async (status, message) => {
    const element = await render();
    login.mockReturnValue(throwError(() => new HttpErrorResponse({ status })));
    fill(element, 'sabine.huber@example.at', 'wrong password');

    await submit(element);

    expect(element.querySelector('[role="alert"]')?.textContent).toContain(message);
    expect(navigateByUrl).not.toHaveBeenCalled();
  });

  it('disables the button while signing in', async () => {
    const element = await render();
    const pending = new Subject<void>();
    login.mockReturnValue(pending);
    fill(element, 'sabine.huber@example.at', 'secret password');

    await submit(element);

    const button = element.querySelector<HTMLButtonElement>('button[type="submit"]')!;
    expect(button.disabled).toBe(true);
    expect(button.textContent).toContain('Anmeldung läuft');
    pending.complete();
  });

  it('shows and hides the password', async () => {
    const element = await render();
    const toggle = element.querySelector<HTMLButtonElement>(
      'button[aria-label="Passwort anzeigen"]',
    )!;

    toggle.click();
    await settle();

    expect(
      element.querySelector('input[autocomplete="current-password"]')?.getAttribute('type'),
    ).toBe('text');
    expect(toggle.getAttribute('aria-label')).toBe('Passwort verbergen');
  });

  it('explains how to get a new password', async () => {
    const element = await render();

    const link = [...element.querySelectorAll('button')].find((button) =>
      button.textContent?.includes('Passwort vergessen?'),
    )!;
    link.click();
    await settle();

    expect(element.textContent).toContain('Bitte wende dich an die Personalabteilung');
  });
});
