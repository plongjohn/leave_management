import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { MATERIAL_ANIMATIONS } from '@angular/material/core';
import { MatDialogRef } from '@angular/material/dialog';
import { Observable, of, throwError } from 'rxjs';
import { provideAppLocale } from '../../../core/i18n/locale.providers';
import { LeaveDataService } from '../../../shared/leave/leave-data.service';
import { SpecialLeaveReasonLimit } from '../../../shared/leave/leave.models';
import { settle } from '../../../testing/settle';
import { LeaveRequestPreview, MyLeaveRequest } from '../my-requests.models';
import { MyRequestsService } from '../my-requests.service';
import { PREVIEW_DEBOUNCE_MS, RequestDialog } from './request-dialog';

describe('RequestDialog', () => {
  const created = { id: 'new', type: 'Vacation' } as MyLeaveRequest;

  const reasons: SpecialLeaveReasonLimit[] = [
    { reason: 'Relocation', maxWorkingDays: 2, period: 'Occasion' },
    { reason: 'CareLeave', maxWorkingDays: 5, period: 'WorkingYear' },
  ];

  const okPreview: LeaveRequestPreview = {
    workingDays: 5,
    holidaysInPeriod: [],
    availableDaysAfter: 8,
    returnedVacationDays: 0,
    errorCode: null,
    needsApproval: true,
    approverName: 'Andrea Steiner',
    absentColleagues: [],
  };

  interface Rendered {
    element: HTMLElement;
    service: {
      submit: ReturnType<typeof vi.fn>;
      reportSickLeave: ReturnType<typeof vi.fn>;
      preview: ReturnType<typeof vi.fn>;
    };
    leaveData: { getHolidays: ReturnType<typeof vi.fn> };
    dialogRef: { close: ReturnType<typeof vi.fn> };
    refresh: () => Promise<void>;
  }

  beforeEach(() => {
    vi.useFakeTimers({ toFake: ['Date'] });
    vi.setSystemTime(new Date(2026, 10, 3, 9, 0));
  });

  afterEach(() => vi.useRealTimers());

  async function render(
    submit: () => Observable<MyLeaveRequest> = () => of(created),
    preview: () => Observable<LeaveRequestPreview> = () => of(okPreview),
  ): Promise<Rendered> {
    const service = {
      submit: vi.fn(submit),
      reportSickLeave: vi.fn(() => of(created)),
      preview: vi.fn(preview),
    };
    const leaveData = {
      getHolidays: vi.fn(() => of([])),
      getSpecialLeaveReasons: () => of(reasons),
    };
    const dialogRef = { close: vi.fn() };

    TestBed.configureTestingModule({
      providers: [
        provideAppLocale(),
        { provide: MATERIAL_ANIMATIONS, useValue: { animationsDisabled: true } },
        { provide: MyRequestsService, useValue: service },
        { provide: LeaveDataService, useValue: leaveData },
        { provide: MatDialogRef, useValue: dialogRef },
        // No waiting in tests; the debounce itself is RxJS behaviour.
        { provide: PREVIEW_DEBOUNCE_MS, useValue: 0 },
      ],
    });

    const fixture = TestBed.createComponent(RequestDialog);
    await fixture.whenStable();
    return {
      element: fixture.nativeElement as HTMLElement,
      service,
      leaveData,
      dialogRef,
      // Twice: the preview timer is only started after the first round of change detection.
      refresh: async () => {
        for (let round = 0; round < 2; round++) {
          await settle();
          await fixture.whenStable();
        }
      },
    };
  }

  function chooseType(element: HTMLElement, label: string): void {
    [...element.querySelectorAll<HTMLLabelElement>('.type-tile')]
      .find((tile) => tile.textContent?.includes(label))!
      .querySelector('input')!
      .click();
  }

  function enterPeriod(element: HTMLElement, start: string, end: string): void {
    for (const [selector, value] of [
      ['input[matStartDate]', start],
      ['input[matEndDate]', end],
    ]) {
      const input = element.querySelector<HTMLInputElement>(selector)!;
      input.value = value;
      input.dispatchEvent(new Event('input'));
      input.dispatchEvent(new Event('blur'));
    }
  }

  function submitButton(element: HTMLElement): HTMLButtonElement {
    return element.querySelector<HTMLButtonElement>('button[type="submit"]')!;
  }

  it('offers the four types with vacation selected', async () => {
    const { element, leaveData } = await render();

    const tiles = [...element.querySelectorAll('.type-tile')];
    expect(tiles.map((tile) => tile.querySelector('span')?.textContent)).toEqual([
      'Urlaub',
      'Zeitausgleich',
      'Krankmeldung',
      'Sonderurlaub',
    ]);
    expect(tiles[0].querySelector('input')?.checked).toBe(true);
    expect(submitButton(element).textContent?.trim()).toBe('Antrag senden');
    expect(leaveData.getHolidays.mock.calls).toEqual([[2025], [2026], [2027]]);
  });

  it('labels the date picker in German', async () => {
    const { element } = await render();

    expect(element.querySelector('mat-datepicker-toggle button')?.getAttribute('aria-label')).toBe(
      'Kalender öffnen',
    );
  });

  it('sends a vacation request and closes with it', async () => {
    const { element, service, dialogRef, refresh } = await render();

    enterPeriod(element, '16.11.2026', '20.11.2026');
    submitButton(element).click();
    await refresh();

    expect(service.submit).toHaveBeenCalledWith({
      startDate: '2026-11-16',
      endDate: '2026-11-20',
      type: 'Vacation',
      specialLeaveReason: null,
      comment: null,
    });
    expect(dialogRef.close).toHaveBeenCalledWith(created);
  });

  it('asks for the period before sending', async () => {
    const { element, service, refresh } = await render();

    submitButton(element).click();
    await refresh();

    expect(element.querySelector('mat-error')?.textContent).toContain(
      'Bitte gib Beginn und Ende an (TT.MM.JJJJ).',
    );
    expect(service.submit).not.toHaveBeenCalled();
  });

  it('does not offer days in the past for vacation', async () => {
    const { element, service, refresh } = await render();

    enterPeriod(element, '02.11.2026', '02.11.2026');
    submitButton(element).click();
    await refresh();

    expect(element.querySelector('mat-error')?.textContent).toContain(
      'Nur eine Krankmeldung darf in der Vergangenheit beginnen.',
    );
    expect(service.submit).not.toHaveBeenCalled();
  });

  it('does not accept an end before the start', async () => {
    const { element, service, refresh } = await render();

    enterPeriod(element, '20.11.2026', '16.11.2026');
    submitButton(element).click();
    await refresh();

    expect(element.querySelector('mat-error')?.textContent).toContain(
      'Das Ende darf nicht vor dem Beginn liegen.',
    );
    expect(service.submit).not.toHaveBeenCalled();
  });

  it('needs a reason for special leave and shows each limit', async () => {
    const { element, service, refresh } = await render();

    chooseType(element, 'Sonderurlaub');
    await refresh();
    enterPeriod(element, '10.12.2026', '11.12.2026');
    submitButton(element).click();
    await refresh();

    expect(element.querySelector('mat-select')).not.toBeNull();
    expect(element.textContent).toContain('Bitte wähle den Anlass.');
    expect(service.submit).not.toHaveBeenCalled();

    element.querySelector<HTMLElement>('.mat-mdc-select-trigger')!.click();
    await refresh();
    const options = [...document.querySelectorAll<HTMLElement>('mat-option')];
    expect(options.map((option) => option.textContent?.trim())).toEqual([
      'Übersiedlung (max. 2 Arbeitstage)',
      'Pflegefreistellung (max. 5 Arbeitstage pro Arbeitsjahr)',
    ]);

    options[0].click();
    submitButton(element).click();
    await refresh();

    expect(service.submit).toHaveBeenCalledWith(
      expect.objectContaining({ type: 'SpecialLeave', specialLeaveReason: 'Relocation' }),
    );
  });

  it('reports sick leave, which may lie in the past, without approval', async () => {
    const { element, service, refresh } = await render();

    chooseType(element, 'Krankmeldung');
    await refresh();
    enterPeriod(element, '02.11.2026', '03.11.2026');
    submitButton(element).click();
    await refresh();

    expect(element.textContent).toContain(
      'Krankmeldungen gelten sofort, eine Genehmigung ist nicht nötig.',
    );
    expect(submitButton(element).textContent?.trim()).toBe('Krankmeldung senden');
    expect(service.reportSickLeave).toHaveBeenCalledWith({
      startDate: '2026-11-02',
      endDate: '2026-11-03',
      comment: null,
      certificate: null,
    });
    expect(service.submit).not.toHaveBeenCalled();
  });

  describe('sick leave during approved vacation', () => {
    const certificateNeeded = (): Observable<LeaveRequestPreview> =>
      of({
        ...okPreview,
        availableDaysAfter: null,
        returnedVacationDays: 3,
        errorCode: 'leave_request.medical_certificate_required',
        needsApproval: false,
        approverName: null,
      });

    async function reportSickLeave(): Promise<Rendered> {
      const rendered = await render(undefined, certificateNeeded);
      chooseType(rendered.element, 'Krankmeldung');
      await rendered.refresh();
      enterPeriod(rendered.element, '16.11.2026', '20.11.2026');
      await rendered.refresh();
      return rendered;
    }

    function attach(element: HTMLElement, file: File): void {
      const input = element.querySelector<HTMLInputElement>('lm-certificate-upload input')!;
      Object.defineProperty(input, 'files', { value: [file] });
      input.dispatchEvent(new Event('change'));
    }

    it('explains why a certificate is needed instead of reporting a violated rule', async () => {
      const { element, service } = await reportSickLeave();

      expect(service.preview).toHaveBeenCalledWith(expect.objectContaining({ type: 'SickLeave' }));
      expect(element.querySelector('.warning')?.textContent).toContain(
        '3 Tage Urlaub bekommst du zurück. Dafür ist eine ärztliche Bestätigung Pflicht.',
      );
      expect(element.querySelector('lm-request-preview [role="alert"]')).toBeNull();
      expect(element.querySelector('lm-certificate-upload .label')?.textContent).toContain('*');
      expect(submitButton(element).disabled).toBe(false);
    });

    it('does not send without the certificate', async () => {
      const { element, service, refresh } = await reportSickLeave();

      submitButton(element).click();
      await refresh();

      expect(element.querySelector('lm-certificate-upload [role="alert"]')?.textContent).toContain(
        'Bitte lade die ärztliche Bestätigung hoch.',
      );
      expect(service.reportSickLeave).not.toHaveBeenCalled();
    });

    it('sends the report together with the certificate', async () => {
      const { element, service, refresh } = await reportSickLeave();
      const pdf = new File(['%PDF-1.7'], 'attest.pdf', { type: 'application/pdf' });

      attach(element, pdf);
      await refresh();
      submitButton(element).click();
      await refresh();

      expect(service.reportSickLeave).toHaveBeenCalledWith({
        startDate: '2026-11-16',
        endDate: '2026-11-20',
        comment: null,
        certificate: pdf,
      });
    });

    it('rejects files the backend would not accept', async () => {
      const { element, service, refresh } = await reportSickLeave();

      attach(element, new File(['GIF89a'], 'foto.gif', { type: 'image/gif' }));
      await refresh();
      submitButton(element).click();
      await refresh();

      expect(element.querySelector('lm-certificate-upload [role="alert"]')?.textContent).toContain(
        'Bitte wähle eine PDF-, JPG- oder PNG-Datei.',
      );
      expect(service.reportSickLeave).not.toHaveBeenCalled();
    });
  });

  it('asks for a preview once the period is complete and shows it', async () => {
    const { element, service, refresh } = await render();

    expect(element.querySelector('lm-request-preview')?.textContent?.trim()).toBe('');

    enterPeriod(element, '16.11.2026', '20.11.2026');
    await refresh();

    expect(service.preview).toHaveBeenCalledExactlyOnceWith({
      startDate: '2026-11-16',
      endDate: '2026-11-20',
      type: 'Vacation',
      specialLeaveReason: null,
    });
    expect(element.querySelector('lm-request-preview')?.textContent).toContain('5 Arbeitstage');
  });

  it('does not ask again when only the comment changes', async () => {
    const { element, service, refresh } = await render();
    enterPeriod(element, '16.11.2026', '20.11.2026');
    await refresh();

    const comment = element.querySelector<HTMLTextAreaElement>('textarea')!;
    comment.value = 'Weihnachtsurlaub';
    comment.dispatchEvent(new Event('input'));
    await refresh();

    expect(service.preview).toHaveBeenCalledTimes(1);
  });

  it('waits for the reason before it asks for a special leave preview', async () => {
    const { element, service, refresh } = await render();

    chooseType(element, 'Sonderurlaub');
    await refresh();
    enterPeriod(element, '10.12.2026', '11.12.2026');
    await refresh();

    expect(service.preview).not.toHaveBeenCalled();
  });

  it('blocks sending while the preview reports a violated rule', async () => {
    const { element, service, refresh } = await render(undefined, () =>
      of({ ...okPreview, availableDaysAfter: -2, errorCode: 'leave_request.insufficient_balance' }),
    );

    enterPeriod(element, '16.11.2026', '20.11.2026');
    await refresh();

    expect(element.querySelector('lm-request-preview [role="alert"]')?.textContent).toContain(
      'Dafür reicht dein Urlaubsanspruch nicht aus.',
    );
    expect(submitButton(element).disabled).toBe(true);
    expect(service.submit).not.toHaveBeenCalled();
  });

  it('shows a violated rule and stays open', async () => {
    const overlapping = new HttpErrorResponse({
      status: 422,
      error: { code: 'leave_request.overlapping' },
    });
    const { element, dialogRef, refresh } = await render(() => throwError(() => overlapping));

    enterPeriod(element, '16.11.2026', '20.11.2026');
    submitButton(element).click();
    await refresh();

    expect(element.querySelector('[role="alert"]')?.textContent).toContain(
      'Der Zeitraum überschneidet sich mit einem anderen Antrag.',
    );
    expect(dialogRef.close).not.toHaveBeenCalled();
    expect(submitButton(element).disabled).toBe(false);
  });
});
