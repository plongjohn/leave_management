import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideAppLocale } from '../../../core/i18n/locale.providers';
import { MyLeaveRequest } from '../my-requests.models';
import { RequestTable } from './request-table';

const vacation: MyLeaveRequest = {
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

/** Hosts the table like the page does, so the two-way bound year can be checked. */
@Component({
  imports: [RequestTable],
  template: `
    <lm-request-table
      [(year)]="year"
      [years]="[2025, 2026, 2027]"
      [requests]="requests()"
      [loading]="loading()"
      [failed]="failed()"
      [cancellingId]="cancellingId()"
      (cancelRequest)="cancelled.push($event)"
    />
  `,
})
class Host {
  readonly year = signal(2026);
  readonly requests = signal<readonly MyLeaveRequest[] | undefined>([]);
  readonly loading = signal(false);
  readonly failed = signal(false);
  readonly cancellingId = signal<string | null>(null);
  readonly cancelled: MyLeaveRequest[] = [];
}

describe('RequestTable', () => {
  async function render(
    setup: (host: Host) => void = () => undefined,
  ): Promise<{ host: Host; element: HTMLElement; refresh: () => Promise<void> }> {
    TestBed.configureTestingModule({ providers: [provideAppLocale()] });
    const fixture = TestBed.createComponent(Host);
    setup(fixture.componentInstance);
    await fixture.whenStable();
    return {
      host: fixture.componentInstance,
      element: fixture.nativeElement as HTMLElement,
      refresh: () => fixture.whenStable(),
    };
  }

  function cells(row: Element): string[] {
    return [...row.querySelectorAll('td')].map((cell) =>
      // Element and text nodes only; Angular leaves comment nodes for @if blocks.
      [...cell.childNodes]
        .filter((node) => node.nodeType !== Node.COMMENT_NODE)
        .map((node) => node.textContent?.trim())
        .filter(Boolean)
        .join(' | '),
    );
  }

  it('shows one row per request', async () => {
    const { element } = await render((host) =>
      host.requests.set([
        { ...vacation, status: 'Pending', approverName: 'Andrea Steiner', decidedByName: null },
        {
          ...vacation,
          id: 'r2',
          startDate: '2026-06-19',
          endDate: '2026-06-19',
          type: 'TimeOffInLieu',
          workingDays: 1,
          canBeCancelled: false,
        },
      ]),
    );

    const rows = [...element.querySelectorAll('tr[mat-row]')];
    expect(rows.map(cells)).toEqual([
      ['16.11. – 20.11.2026', 'Urlaub', '5', 'Offen', 'Stornieren'],
      ['19.06.2026', 'Zeitausgleich', '1', 'Genehmigt', ''],
    ]);
  });

  it.each([
    [{ type: 'SpecialLeave', specialLeaveReason: 'Relocation' }, 'Sonderurlaub | Übersiedlung'],
    [{ returnedDays: 2 }, 'Urlaub | davon 2 Tage wegen Krankheit zurück'],
    [
      { type: 'SickLeave', hasMedicalCertificate: true },
      'Krankmeldung | mit ärztlicher Bestätigung',
    ],
    [
      { type: 'SickLeave', hasMedicalCertificate: true, isCertificateRevoked: true },
      'Krankmeldung | Bestätigung nicht anerkannt',
    ],
  ] as const)('adds a note under the type: %o', async (overrides, expected) => {
    const { element } = await render((host) => host.requests.set([{ ...vacation, ...overrides }]));

    expect(cells(element.querySelector('tr[mat-row]')!)[1]).toBe(expected);
  });

  it('emits the request to cancel and disables its button while cancelling', async () => {
    const { host, element, refresh } = await render((host) => host.requests.set([vacation]));
    const button = element.querySelector<HTMLButtonElement>('button[aria-label]')!;

    expect(button.getAttribute('aria-label')).toBe('Antrag 16.11. – 20.11.2026 stornieren');

    button.click();
    expect(host.cancelled).toEqual([vacation]);

    host.cancellingId.set('r1');
    await refresh();
    expect(button.disabled).toBe(true);
  });

  it('changes the year with the filter', async () => {
    const { host, element, refresh } = await render();
    const toggles = [...element.querySelectorAll<HTMLButtonElement>('mat-button-toggle button')];

    expect(toggles.map((toggle) => toggle.textContent?.trim())).toEqual(['2025', '2026', '2027']);
    expect(toggles[1].getAttribute('aria-checked')).toBe('true');

    toggles[0].click();
    await refresh();

    expect(host.year()).toBe(2025);
  });

  it('says when a year has no requests', async () => {
    const { element } = await render();

    expect(element.querySelector('.message')?.textContent).toBe('Keine Anträge in 2026.');
  });

  it('says when the requests cannot be loaded', async () => {
    const { element } = await render((host) => host.failed.set(true));

    expect(element.querySelector('[role="alert"]')?.textContent).toContain(
      'Die Anträge konnten nicht geladen werden.',
    );
  });
});
