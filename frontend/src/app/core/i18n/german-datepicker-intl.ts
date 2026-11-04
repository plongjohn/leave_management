import { Injectable } from '@angular/core';
import { MatDatepickerIntl } from '@angular/material/datepicker';

/** German labels of the Material date picker; mostly read by screen readers. */
@Injectable()
export class GermanDatepickerIntl extends MatDatepickerIntl {
  override calendarLabel = 'Kalender';
  override openCalendarLabel = 'Kalender öffnen';
  override closeCalendarLabel = 'Kalender schließen';
  override prevMonthLabel = 'Vorheriger Monat';
  override nextMonthLabel = 'Nächster Monat';
  override prevYearLabel = 'Vorheriges Jahr';
  override nextYearLabel = 'Nächstes Jahr';
  override prevMultiYearLabel = 'Vorherige 24 Jahre';
  override nextMultiYearLabel = 'Nächste 24 Jahre';
  override switchToMonthViewLabel = 'Monat auswählen';
  override switchToMultiYearViewLabel = 'Jahr und Monat auswählen';
  override startDateLabel = 'Beginn';
  override endDateLabel = 'Ende';
  override comparisonDateLabel = 'Vergleichszeitraum';

  override formatYearRange(start: string, end: string): string {
    return `${start} – ${end}`;
  }

  override formatYearRangeLabel(start: string, end: string): string {
    return `${start} bis ${end}`;
  }
}

/**
 * Add to the `providers` of every component with a date picker. Not app-wide on purpose: the
 * root injector would pull the whole date picker into the initial bundle (+300 kB).
 */
export const GERMAN_DATEPICKER_INTL = {
  provide: MatDatepickerIntl,
  useClass: GermanDatepickerIntl,
};
