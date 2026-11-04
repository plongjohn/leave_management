import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { LeaveDataService } from './leave-data.service';

describe('LeaveDataService', () => {
  let service: LeaveDataService;
  let httpTesting: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(LeaveDataService);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpTesting.verify());

  it('loads the holidays of one year', async () => {
    const holidays = firstValueFrom(service.getHolidays(2026));

    const request = httpTesting.expectOne('/api/holidays?year=2026');
    expect(request.request.method).toBe('GET');
    request.flush([{ date: '2026-01-01', name: 'Neujahr', kind: 'Statutory' }]);

    expect(await holidays).toEqual([{ date: '2026-01-01', name: 'Neujahr', kind: 'Statutory' }]);
  });

  it('loads the special leave reasons with their limits', async () => {
    const reasons = firstValueFrom(service.getSpecialLeaveReasons());

    httpTesting
      .expectOne('/api/special-leave-reasons')
      .flush([{ reason: 'Relocation', maxWorkingDays: 2, period: 'Occasion' }]);

    expect(await reasons).toEqual([
      { reason: 'Relocation', maxWorkingDays: 2, period: 'Occasion' },
    ]);
  });
});
