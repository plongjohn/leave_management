import { registerLocaleData } from '@angular/common';
import localeDeAt from '@angular/common/locales/de-AT';
import { EnvironmentProviders, LOCALE_ID, makeEnvironmentProviders } from '@angular/core';
import { MAT_DATE_LOCALE, MatDateFormats } from '@angular/material/core';
import { provideDateFnsAdapter } from '@angular/material-date-fns-adapter';
import { deAT } from 'date-fns/locale';

/** Formats for Material date pickers (date-fns tokens). */
export const APP_DATE_FORMATS: MatDateFormats = {
  parse: {
    // "d" and "M" accept one or two digits, so both "4.11.2026" and "04.11.2026" work.
    dateInput: 'd.M.yyyy',
  },
  display: {
    dateInput: 'dd.MM.yyyy',
    monthYearLabel: 'MMM yyyy',
    dateA11yLabel: 'PPPP',
    monthYearA11yLabel: 'LLLL yyyy',
  },
};

/**
 * German (Austria) for the whole app: Angular pipes (`date`, `number`) via `LOCALE_ID`,
 * Material date pickers via the date-fns adapter. The week starts on Monday (part of `deAT`).
 */
export function provideAppLocale(): EnvironmentProviders {
  registerLocaleData(localeDeAt);

  return makeEnvironmentProviders([
    { provide: LOCALE_ID, useValue: 'de-AT' },
    { provide: MAT_DATE_LOCALE, useValue: deAT },
    provideDateFnsAdapter(APP_DATE_FORMATS),
  ]);
}
