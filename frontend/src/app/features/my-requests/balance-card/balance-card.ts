import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, computed, input } from '@angular/core';
import { APP_LOCALE } from '../../../core/i18n/locale.providers';
import { EntitlementYear, LeaveBalance } from '../my-requests.models';
import { balanceCardTexts } from '../my-requests.texts';

const texts = balanceCardTexts;
const dayFormat = new Intl.NumberFormat(APP_LOCALE, { maximumFractionDigits: 1 });

/** One entitlement year with the widths of its bar segments. */
interface YearRow {
  year: EntitlementYear;
  /** CSS widths in percent of the entitled days. */
  approvedWidth: string;
  pendingWidth: string;
  /** The bar for screen readers. */
  barLabel: string;
}

/** The entitlement years that have not expired, with taken and requested days (presentational). */
@Component({
  selector: 'lm-balance-card',
  imports: [DatePipe, DecimalPipe],
  templateUrl: './balance-card.html',
  styleUrl: './balance-card.scss',
})
export class BalanceCard {
  readonly balance = input.required<LeaveBalance>();

  protected readonly texts = texts;
  protected readonly rows = computed(() => this.balance().years.map(toRow));
}

function toRow(year: EntitlementYear): YearRow {
  // A negative remainder (revoked certificate) or an entitlement of 0 days must not break the bar.
  const percent = (days: number) =>
    year.entitledDays > 0 ? Math.max(0, (days / year.entitledDays) * 100) : 0;
  const approved = Math.min(100, percent(year.approvedDays));
  const pending = Math.min(100 - approved, percent(year.pendingDays));

  return {
    year,
    approvedWidth: `${approved}%`,
    pendingWidth: `${pending}%`,
    barLabel: texts.barLabel(
      year.year,
      dayFormat.format(year.approvedDays),
      dayFormat.format(year.pendingDays),
      dayFormat.format(year.remainingDays),
    ),
  };
}
