import { TestBed } from '@angular/core/testing';
import { provideAppLocale } from '../../../core/i18n/locale.providers';
import { LeaveRequestPreview } from '../my-requests.models';
import { RequestPreview } from './request-preview';

describe('RequestPreview', () => {
  const christmas: LeaveRequestPreview = {
    workingDays: 7,
    holidaysInPeriod: [
      { date: '2026-12-24', name: 'Heiliger Abend', kind: 'CompanyHalfDay' },
      { date: '2026-12-25', name: 'Christtag', kind: 'Statutory' },
      { date: '2026-12-26', name: 'Stefanitag', kind: 'Statutory' },
      { date: '2026-12-31', name: 'Silvester', kind: 'CompanyHalfDay' },
    ],
    availableDaysAfter: 1,
    returnedVacationDays: 0,
    errorCode: null,
    needsApproval: true,
    approverName: 'Andrea Steiner',
    absentColleagues: [
      { employeeId: 'e3', name: 'Julia Pichler' },
      { employeeId: 'e4', name: 'Sabine Huber' },
    ],
  };

  async function render(
    preview: LeaveRequestPreview | undefined,
    state: { loading?: boolean; failed?: boolean } = {},
  ): Promise<HTMLElement> {
    TestBed.configureTestingModule({ providers: [provideAppLocale()] });
    const fixture = TestBed.createComponent(RequestPreview);
    fixture.componentRef.setInput('preview', preview);
    fixture.componentRef.setInput('loading', state.loading ?? false);
    fixture.componentRef.setInput('failed', state.failed ?? false);
    await fixture.whenStable();
    return fixture.nativeElement as HTMLElement;
  }

  function text(element: Element | null): string {
    return (element?.textContent ?? '').replace(/\s+/g, ' ').trim();
  }

  it('shows working days, the balance afterwards and why days are not counted', async () => {
    const element = await render(christmas);

    expect(text(element.querySelector('.days'))).toBe('7 Arbeitstage');
    expect(text(element.querySelector('.balance'))).toBe('danach 1 Tag verfügbar');
    expect([...element.querySelectorAll('.note')].map(text)).toEqual([
      'Nicht gezählt: 25.12. Christtag, 26.12. Stefanitag.',
      'Halbe Arbeitstage: 24.12. und 31.12.',
    ]);
  });

  it('names absent colleagues and who decides', async () => {
    const element = await render(christmas);

    const colleagues = element.querySelector('.colleagues')!;
    expect([...colleagues.querySelectorAll('.avatar')].map(text)).toEqual(['JP', 'SH']);
    expect(text(colleagues.lastElementChild)).toBe(
      'Julia Pichler und Sabine Huber sind in diesem Zeitraum ebenfalls abwesend.',
    );
    expect(text(element.querySelector('.info'))).toContain(
      'Andrea Steiner entscheidet über deinen Antrag.',
    );
  });

  it('names HR when the employee has no manager', async () => {
    const element = await render({ ...christmas, approverName: null, absentColleagues: [] });

    expect(text(element.querySelector('.info'))).toContain(
      'Die Personalabteilung entscheidet über deinen Antrag.',
    );
    expect(element.querySelector('.colleagues')).toBeNull();
  });

  it('shows returned vacation for sick leave and nobody who decides', async () => {
    const element = await render({
      ...christmas,
      holidaysInPeriod: [],
      availableDaysAfter: null,
      returnedVacationDays: 3,
      needsApproval: false,
      approverName: null,
    });

    expect(text(element.querySelector('.balance'))).toBe('3 Tage Urlaub bekommst du zurück');
    expect(element.querySelector('.info')).toBeNull();
  });

  it('says there is no deduction for other types', async () => {
    const element = await render({ ...christmas, availableDaysAfter: null });

    expect(text(element.querySelector('.balance'))).toBe('kein Urlaubsabzug');
  });

  it('warns about a violated rule', async () => {
    const element = await render({ ...christmas, errorCode: 'leave_request.overlapping' });

    expect(element.querySelector('.summary')?.classList).toContain('summary--error');
    expect(text(element.querySelector('[role="alert"]'))).toContain(
      'Der Zeitraum überschneidet sich mit einem anderen Antrag.',
    );
  });

  it('shows progress while checking and a hint when the preview fails', async () => {
    expect(
      (await render(undefined, { loading: true })).querySelector('mat-progress-bar'),
    ).not.toBeNull();

    TestBed.resetTestingModule();
    expect(text(await render(undefined, { failed: true }))).toBe(
      'Die Vorschau ist gerade nicht verfügbar. Du kannst trotzdem senden.',
    );
  });
});
