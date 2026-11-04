import { format, parseISO } from 'date-fns';
import { APP_LOCALE } from '../../core/i18n/locale.providers';

const dayCountFormat = new Intl.NumberFormat(APP_LOCALE, { maximumFractionDigits: 1 });

/**
 * A period as the employees read it: "19.06.2026", "16.11. – 20.11.2026" or, across the turn of
 * the year, "28.12.2026 – 04.01.2027". Takes ISO dates (yyyy-MM-dd).
 */
export function formatPeriod(startDate: string, endDate: string): string {
  const start = parseISO(startDate);
  const end = parseISO(endDate);

  if (startDate === endDate) {
    return format(start, 'dd.MM.yyyy');
  }

  const startFormat = start.getFullYear() === end.getFullYear() ? 'dd.MM.' : 'dd.MM.yyyy';
  return `${format(start, startFormat)} – ${format(end, 'dd.MM.yyyy')}`;
}

/** "1 Arbeitstag", "7,5 Arbeitstage" – half days come from company half days (24./31.12.). */
export function formatDayCount(days: number, singular: string, plural: string): string {
  return `${dayCountFormat.format(days)} ${days === 1 ? singular : plural}`;
}
