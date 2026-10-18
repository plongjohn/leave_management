import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { EmployeeListItem } from '../employee';
import { EmployeeList } from './employee-list';

describe('EmployeeList', () => {
  let httpTesting: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [EmployeeList],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpTesting.verify());

  async function render(response: EmployeeListItem[] | 'error'): Promise<HTMLElement> {
    const fixture = TestBed.createComponent(EmployeeList);
    fixture.detectChanges();

    const request = httpTesting.expectOne('/api/employees');
    if (response === 'error') {
      request.flush(null, { status: 500, statusText: 'Server Error' });
    } else {
      request.flush(response);
    }

    await fixture.whenStable();
    return fixture.nativeElement as HTMLElement;
  }

  it('shows one row per employee', async () => {
    const element = await render([
      {
        id: '1',
        firstName: 'Anna',
        lastName: 'Bauer',
        email: 'anna.bauer@example.at',
        entryDate: '2022-04-01',
      },
      {
        id: '2',
        firstName: 'Lena',
        lastName: 'Wagner',
        email: 'lena.wagner@example.at',
        entryDate: '2020-02-01',
      },
    ]);

    const rows = element.querySelectorAll('tr[mat-row]');
    expect(rows.length).toBe(2);
    expect(rows[0].textContent).toContain('Bauer Anna');
    expect(rows[0].textContent).toContain('01.04.2022');
  });

  it('shows a message when there are no employees', async () => {
    const element = await render([]);

    expect(element.textContent).toContain('Es sind noch keine Mitarbeiter erfasst.');
  });

  it('shows an error when loading fails', async () => {
    const element = await render('error');

    expect(element.querySelector('[role="alert"]')?.textContent).toContain(
      'Die Mitarbeiter konnten nicht geladen werden.',
    );
  });
});
