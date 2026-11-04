import { BreakpointObserver, BreakpointState } from '@angular/cdk/layout';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { CurrentUser } from '../../auth/auth.models';
import { AuthService } from '../../auth/auth.service';
import { settle } from '../../../testing/settle';
import { AppShell } from './app-shell';

describe('AppShell', () => {
  const markus: CurrentUser = {
    employeeId: 'e2',
    firstName: 'Markus',
    lastName: 'Gruber',
    email: 'markus.gruber@example.at',
    role: 'Manager',
  };

  async function render(
    options: { compact?: boolean } = {},
  ): Promise<{ element: HTMLElement; logout: ReturnType<typeof vi.fn> }> {
    const logout = vi.fn(() => of(undefined));
    const layout: BreakpointState = { matches: options.compact ?? false, breakpoints: {} };
    TestBed.configureTestingModule({
      imports: [AppShell],
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: { currentUser: signal(markus).asReadonly(), logout } },
        { provide: BreakpointObserver, useValue: { observe: () => of(layout) } },
      ],
    });

    const fixture = TestBed.createComponent(AppShell);
    await fixture.whenStable();
    return { element: fixture.nativeElement as HTMLElement, logout };
  }

  function menuButton(element: HTMLElement): HTMLButtonElement | null {
    return element.querySelector('button[aria-label="Navigation ein- oder ausblenden"]');
  }

  it('shows the signed-in employee with the German role name', async () => {
    const { element } = await render();

    expect(element.querySelector('.user')?.textContent).toContain('Markus Gruber');
    expect(element.querySelector('.user')?.textContent).toContain('Führungskraft');
  });

  it('lists the navigation entries as links', async () => {
    const { element } = await render();

    const links = [...element.querySelectorAll('nav a')];
    expect(links.map((link) => link.querySelector('[matListItemTitle]')?.textContent)).toEqual([
      'Übersicht',
      'Mitarbeiter',
    ]);
    expect(links[0].getAttribute('href')).toBe('/overview');
  });

  it('keeps the navigation open beside the content on wide screens', async () => {
    const { element } = await render();

    expect(menuButton(element)).toBeNull();
    expect(element.querySelector('mat-sidenav')?.classList).toContain('mat-drawer-side');
  });

  it('hides the navigation behind a menu button on small screens', async () => {
    const { element } = await render({ compact: true });
    const button = menuButton(element)!;

    expect(button.getAttribute('aria-expanded')).toBe('false');

    button.click();
    await settle();

    expect(element.querySelector('mat-sidenav')?.classList).toContain('mat-drawer-opened');
    expect(button.getAttribute('aria-expanded')).toBe('true');
  });

  it('signs out and opens the login page', async () => {
    const { element, logout } = await render();
    const navigateByUrl = vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);

    const signOut = [...element.querySelectorAll('button')].find((button) =>
      button.textContent?.includes('Abmelden'),
    )!;
    signOut.click();
    await settle();

    expect(logout).toHaveBeenCalled();
    expect(navigateByUrl).toHaveBeenCalledWith('/login');
  });
});
