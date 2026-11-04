import { TestBed } from '@angular/core/testing';
import { provideAppLocale } from '../../../core/i18n/locale.providers';
import { EntitlementYear } from '../my-requests.models';
import { BalanceCard } from './balance-card';

describe('BalanceCard', () => {
  const year2025: EntitlementYear = {
    year: 2025,
    entitledDays: 25,
    approvedDays: 25,
    pendingDays: 0,
    remainingDays: 0,
    expiresOn: '2027-12-31',
  };
  const year2026: EntitlementYear = {
    year: 2026,
    entitledDays: 25,
    approvedDays: 10,
    pendingDays: 7.5,
    remainingDays: 7.5,
    expiresOn: '2028-12-31',
  };

  async function render(years: EntitlementYear[]): Promise<HTMLElement> {
    TestBed.configureTestingModule({ providers: [provideAppLocale()] });
    const fixture = TestBed.createComponent(BalanceCard);
    fixture.componentRef.setInput('balance', { availableDays: 7.5, years });
    await fixture.whenStable();
    return fixture.nativeElement as HTMLElement;
  }

  function text(element: Element | null): string {
    return (element?.textContent ?? '').replace(/\s+/g, ' ').trim();
  }

  it('shows each entitlement year with expiry and figures, oldest first', async () => {
    const element = await render([year2025, year2026]);
    const years = [...element.querySelectorAll('.year')];

    expect(
      years.map((year) =>
        [...year.querySelector('.year-heading')!.children].map((part) => text(part)).join(' '),
      ),
    ).toEqual(['Anspruch 2025 verfällt 31.12.2027', 'Anspruch 2026 verfällt 31.12.2028']);
    expect([...years[1].querySelectorAll('.figures > span')].map((figure) => text(figure))).toEqual(
      ['25 Anspruch', '10 genommen', '7,5 beantragt', '7,5 übrig'],
    );
  });

  it('draws taken and requested days as parts of the bar, labelled for screen readers', async () => {
    const element = await render([year2026]);
    const bar = element.querySelector<HTMLElement>('.bar')!;

    expect(bar.getAttribute('aria-label')).toBe('2026: 10 genommen, 7,5 beantragt, 7,5 übrig');
    expect(bar.querySelector<HTMLElement>('.bar-approved')!.style.width).toBe('40%');
    expect(bar.querySelector<HTMLElement>('.bar-pending')!.style.width).toBe('30%');
  });

  it('keeps the bar within its width and marks a negative remainder', async () => {
    const element = await render([
      { ...year2026, approvedDays: 27, pendingDays: 2, remainingDays: -4 },
    ]);

    expect(element.querySelector<HTMLElement>('.bar-approved')!.style.width).toBe('100%');
    expect(element.querySelector<HTMLElement>('.bar-pending')!.style.width).toBe('0%');
    expect(element.querySelector('.remaining')?.classList).toContain('remaining--negative');
  });

  it('says when there is no entitlement yet', async () => {
    const element = await render([]);

    expect(text(element)).toContain('Für dich ist noch kein Urlaubsanspruch angelegt.');
    expect(element.querySelector('.legend')).toBeNull();
  });
});
