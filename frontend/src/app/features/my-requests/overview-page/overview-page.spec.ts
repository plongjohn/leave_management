import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
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

  const januaryVacation: MyLeaveRequest = {
    id: 'r1',
    startDate: '2027-01-11',
    endDate: '2027-01-15',
    type: 'Vacation',
    specialLeaveReason: null,
    workingDays: 5,
    returnedDays: 0,
    status: 'Pending',
    comment: null,
    hasMedicalCertificate: false,
    isCertificateRevoked: false,
    createdAt: '2026-11-01T08:00:00+01:00',
    approverName: 'Andrea Steiner',
    decidedByName: null,
    decidedAt: null,
    canBeCancelled: true,
    version: 1,
  };

  beforeEach(() => {
    vi.useFakeTimers({ toFake: ['Date'] });
    vi.setSystemTime(new Date(2026, 10, 3, 9, 0));
  });

  afterEach(() => vi.useRealTimers());

  async function render(
    getRequests: (year: number) => Observable<MyLeaveRequest[]>,
  ): Promise<HTMLElement> {
    TestBed.configureTestingModule({
      providers: [
        provideAppLocale(),
        { provide: AuthService, useValue: { currentUser: signal(markus).asReadonly() } },
        {
          provide: MyRequestsService,
          useValue: { getBalance: () => of(balance), getRequests: vi.fn(getRequests) },
        },
      ],
    });

    const fixture = TestBed.createComponent(OverviewPage);
    await fixture.whenStable();
    return fixture.nativeElement as HTMLElement;
  }

  it('greets the employee with today’s date', async () => {
    const element = await render(() => of([]));

    expect(element.querySelector('h1')?.textContent).toBe('Hallo Markus');
    expect(element.querySelector('.date')?.textContent).toBe('Dienstag, 3. November 2026');
  });

  it('loads this and next year, so a request for January shows up', async () => {
    const element = await render((year) => of(year === 2027 ? [januaryVacation] : []));

    expect(TestBed.inject(MyRequestsService).getRequests).toHaveBeenCalledWith(2026);
    expect(TestBed.inject(MyRequestsService).getRequests).toHaveBeenCalledWith(2027);
    expect(element.querySelector('lm-overview-tiles')?.textContent).toContain(
      'wartet auf Andrea Steiner',
    );
    expect(element.querySelector('lm-overview-tiles')?.textContent).toContain(
      '11.01. – 15.01.2027',
    );
  });

  it('says when the overview cannot be loaded', async () => {
    const element = await render(() => throwError(() => new Error('offline')));

    expect(element.querySelector('[role="alert"]')?.textContent).toContain(
      'Die Übersicht konnte nicht geladen werden.',
    );
    expect(element.querySelector('lm-overview-tiles')).toBeNull();
  });
});
