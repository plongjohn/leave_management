import { HttpErrorResponse } from '@angular/common/http';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Observable, of, throwError } from 'rxjs';
import { CurrentUser } from '../../../core/auth/auth.models';
import { AuthService } from '../../../core/auth/auth.service';
import { provideAppLocale } from '../../../core/i18n/locale.providers';
import { LeaveBalance, MyLeaveRequest } from '../my-requests.models';
import { MyRequestsService } from '../my-requests.service';
import { OverviewPage } from './overview-page';

describe('OverviewPage', () => {
  const markus: CurrentUser = {
    employeeId: 'e2',
    firstName: 'Markus',
    lastName: 'Gruber',
    email: 'markus.gruber@example.at',
    role: 'Manager',
  };

  const balance: LeaveBalance = { availableDays: 8, years: [] };

  const novemberVacation: MyLeaveRequest = {
    id: 'r1',
    startDate: '2026-11-16',
    endDate: '2026-11-20',
    type: 'Vacation',
    specialLeaveReason: null,
    workingDays: 5,
    returnedDays: 0,
    status: 'Approved',
    comment: null,
    hasMedicalCertificate: false,
    isCertificateRevoked: false,
    createdAt: '2026-10-01T08:00:00+02:00',
    approverName: null,
    decidedByName: 'Andrea Steiner',
    decidedAt: '2026-10-02T08:00:00+02:00',
    canBeCancelled: true,
    version: 7,
  };

  const januaryVacation: MyLeaveRequest = {
    ...novemberVacation,
    id: 'r2',
    startDate: '2027-01-11',
    endDate: '2027-01-15',
    status: 'Pending',
    approverName: 'Andrea Steiner',
    decidedByName: null,
    decidedAt: null,
  };

  interface Setup {
    getRequests?: (year: number) => Observable<MyLeaveRequest[]>;
    cancel?: () => Observable<void>;
    confirmed?: boolean;
  }

  interface Rendered {
    element: HTMLElement;
    service: { getRequests: ReturnType<typeof vi.fn>; cancel: ReturnType<typeof vi.fn> };
    dialog: { open: ReturnType<typeof vi.fn> };
    snackBar: { open: ReturnType<typeof vi.fn> };
    refresh: () => Promise<void>;
  }

  beforeEach(() => {
    vi.useFakeTimers({ toFake: ['Date'] });
    vi.setSystemTime(new Date(2026, 10, 3, 9, 0));
  });

  afterEach(() => vi.useRealTimers());

  async function render(setup: Setup = {}): Promise<Rendered> {
    const service = {
      getBalance: () => of(balance),
      getRequests: vi.fn(
        setup.getRequests ?? ((year: number) => of(year === 2026 ? [novemberVacation] : [])),
      ),
      cancel: vi.fn(setup.cancel ?? (() => of(undefined))),
    };
    const dialog = { open: vi.fn(() => ({ afterClosed: () => of(setup.confirmed ?? true) })) };
    const snackBar = { open: vi.fn() };

    TestBed.configureTestingModule({
      providers: [
        provideAppLocale(),
        { provide: AuthService, useValue: { currentUser: signal(markus).asReadonly() } },
        { provide: MyRequestsService, useValue: service },
        { provide: MatDialog, useValue: dialog },
        { provide: MatSnackBar, useValue: snackBar },
      ],
    });

    const fixture = TestBed.createComponent(OverviewPage);
    await fixture.whenStable();
    return {
      element: fixture.nativeElement as HTMLElement,
      service,
      dialog,
      snackBar,
      refresh: () => fixture.whenStable(),
    };
  }

  function cancelButton(element: HTMLElement): HTMLButtonElement {
    return element.querySelector<HTMLButtonElement>('lm-request-table td button')!;
  }

  it('greets the employee with today’s date', async () => {
    const { element } = await render();

    expect(element.querySelector('h1')?.textContent).toBe('Hallo Markus');
    expect(element.querySelector('.date')?.textContent).toBe('Dienstag, 3. November 2026');
  });

  it('loads this and next year for the tiles, so a request for January shows up', async () => {
    const { element, service } = await render({
      getRequests: (year) => of(year === 2027 ? [januaryVacation] : []),
    });

    expect(service.getRequests).toHaveBeenCalledWith(2027);
    expect(element.querySelector('lm-overview-tiles')?.textContent).toContain(
      'wartet auf Andrea Steiner',
    );
  });

  it('says when the overview cannot be loaded', async () => {
    const { element } = await render({ getRequests: () => throwError(() => new Error('offline')) });

    expect(element.querySelector('[role="alert"]')?.textContent).toContain(
      'Die Übersicht konnte nicht geladen werden.',
    );
    expect(element.querySelector('lm-overview-tiles')).toBeNull();
  });

  it('lists the requests of this year and loads another year when it is selected', async () => {
    const { element, service, refresh } = await render();

    expect(element.querySelector('lm-request-table td')?.textContent).toContain(
      '16.11. – 20.11.2026',
    );

    service.getRequests.mockClear();
    element.querySelector<HTMLButtonElement>('mat-button-toggle button')!.click();
    await refresh();

    expect(service.getRequests).toHaveBeenCalledExactlyOnceWith(2025);
    expect(element.querySelector('lm-request-table .message')?.textContent).toBe(
      'Keine Anträge in 2025.',
    );
  });

  it('cancels after confirmation with the version the user saw and reloads', async () => {
    const { element, service, dialog, snackBar, refresh } = await render();

    service.getRequests.mockClear();
    cancelButton(element).click();
    await refresh();

    expect(dialog.open).toHaveBeenCalledWith(expect.anything(), {
      width: '420px',
      data: expect.objectContaining({
        message:
          'Urlaub 16.11. – 20.11.2026 wird storniert. Die Tage bekommst du wieder gutgeschrieben.',
      }),
    });
    expect(service.cancel).toHaveBeenCalledWith(novemberVacation);
    expect(snackBar.open).toHaveBeenCalledWith('Der Antrag wurde storniert.', 'OK', {
      duration: 6000,
    });
    expect(service.getRequests).toHaveBeenCalledWith(2026);
  });

  it('keeps the request when the user does not confirm', async () => {
    const { element, service, refresh } = await render({ confirmed: false });

    cancelButton(element).click();
    await refresh();

    expect(service.cancel).not.toHaveBeenCalled();
  });

  it('reloads and explains when the request was changed in the meantime', async () => {
    const conflict = new HttpErrorResponse({
      status: 409,
      error: { code: 'concurrency_conflict' },
    });
    const { element, service, snackBar, refresh } = await render({
      cancel: () => throwError(() => conflict),
    });

    service.getRequests.mockClear();
    cancelButton(element).click();
    await refresh();

    expect(snackBar.open).toHaveBeenCalledWith(
      'Der Antrag wurde inzwischen geändert. Die Liste wurde neu geladen.',
      'OK',
      { duration: 6000 },
    );
    expect(service.getRequests).toHaveBeenCalledWith(2026);
    expect(cancelButton(element).disabled).toBe(false);
  });

  it('does not reload when the server cannot be reached', async () => {
    const offline = new HttpErrorResponse({ status: 0, error: new ProgressEvent('error') });
    const { element, service, snackBar, refresh } = await render({
      cancel: () => throwError(() => offline),
    });

    service.getRequests.mockClear();
    cancelButton(element).click();
    await refresh();

    expect(snackBar.open).toHaveBeenCalledWith(
      'Das hat leider nicht geklappt. Bitte versuche es später noch einmal.',
      'OK',
      { duration: 6000 },
    );
    expect(service.getRequests).not.toHaveBeenCalled();
  });
});
