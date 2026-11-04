import { TestBed } from '@angular/core/testing';
import { provideAppLocale } from '../../../core/i18n/locale.providers';
import { MyLeaveRequest } from '../my-requests.models';
import { OverviewSummary } from '../overview-summary';
import { OverviewTiles } from './overview-tiles';

describe('OverviewTiles', () => {
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
    version: 1,
  };

  const summary: OverviewSummary = {
    availableDays: 8,
    pendingVacationDays: 7,
    pendingRequests: 1,
    approvers: ['Andrea Steiner'],
    nextAbsence: { request: vacation, daysUntilStart: 13 },
  };

  async function render(overrides: Partial<OverviewSummary> = {}): Promise<string[]> {
    TestBed.configureTestingModule({ providers: [provideAppLocale()] });
    const fixture = TestBed.createComponent(OverviewTiles);
    fixture.componentRef.setInput('summary', { ...summary, ...overrides });
    await fixture.whenStable();

    // One string per tile, its lines (label, value, hint) separated by a space.
    const element = fixture.nativeElement as HTMLElement;
    return [...element.querySelectorAll('.tile')].map((tile) =>
      [...tile.children].map((line) => normalize(line.textContent)).join(' '),
    );
  }

  function normalize(text: string | null): string {
    return (text ?? '').replace(/\s+/g, ' ').trim();
  }

  it('shows the key figures', async () => {
    const [available, open, next] = await render();

    expect(available).toBe('Urlaub verfügbar 8 Tage nach Abzug von 7 beantragten Tagen');
    expect(open).toBe('Offene Anträge 1 wartet auf Andrea Steiner');
    expect(next).toBe(
      'Nächste Abwesenheit 16.11. – 20.11.2026 Urlaub · 5 Arbeitstage · in 13 Tagen',
    );
  });

  it('shows half days and an overdrawn balance', async () => {
    const [available] = await render({ availableDays: -1.5 });

    expect(available).toBe('Urlaub verfügbar -1,5 Tage mehr Urlaub verplant als vorhanden');
  });

  it.each([
    [{ pendingRequests: 0, approvers: [] }, 'Offene Anträge 0 nichts offen'],
    [{ approvers: [null] }, 'Offene Anträge 1 wartet auf die Personalabteilung'],
    [
      { pendingRequests: 2, approvers: ['Andrea Steiner', null] },
      'Offene Anträge 2 warten auf Entscheidung',
    ],
  ] as const)('names who decides: %o', async (overrides, expected) => {
    const [, open] = await render({ ...overrides, approvers: [...overrides.approvers] });

    expect(open).toBe(expected);
  });

  it.each([
    [0, 'ab heute'],
    [1, 'morgen'],
    [-2, 'läuft gerade'],
  ])('describes a start in %d days as "%s"', async (daysUntilStart, expected) => {
    const [, , next] = await render({ nextAbsence: { request: vacation, daysUntilStart } });

    expect(next).toContain(expected);
  });

  it('marks a pending next absence as requested', async () => {
    const [, , next] = await render({
      nextAbsence: { request: { ...vacation, status: 'Pending' }, daysUntilStart: 13 },
    });

    expect(next).toContain('Urlaub (beantragt) · 5 Arbeitstage');
  });

  it('says when no absence is planned', async () => {
    const [, , next] = await render({ nextAbsence: null });

    expect(next).toBe('Nächste Abwesenheit keine geplant');
  });
});
