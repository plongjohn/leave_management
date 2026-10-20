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

  async function render(): Promise<{ element: HTMLElement; logout: ReturnType<typeof vi.fn> }> {
    const logout = vi.fn(() => of(undefined));
    TestBed.configureTestingModule({
      imports: [AppShell],
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: { currentUser: signal(markus).asReadonly(), logout } },
      ],
    });

    const fixture = TestBed.createComponent(AppShell);
    await fixture.whenStable();
    return { element: fixture.nativeElement as HTMLElement, logout };
  }

  it('shows the signed-in employee with the German role name', async () => {
    const { element } = await render();

    expect(element.querySelector('.user')?.textContent).toContain('Markus Gruber');
    expect(element.querySelector('.user')?.textContent).toContain('Führungskraft');
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
