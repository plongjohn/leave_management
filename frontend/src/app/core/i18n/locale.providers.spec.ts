import { DatePipe } from '@angular/common';
import { LOCALE_ID } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { DateAdapter, MAT_DATE_FORMATS, MatDateFormats } from '@angular/material/core';
import { provideAppLocale } from './locale.providers';

describe('provideAppLocale', () => {
  let adapter: DateAdapter<Date>;
  let formats: MatDateFormats;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideAppLocale()] });
    adapter = TestBed.inject(DateAdapter);
    formats = TestBed.inject(MAT_DATE_FORMATS);
  });

  it('sets de-AT as the Angular locale', () => {
    expect(TestBed.inject(LOCALE_ID)).toBe('de-AT');
    expect(new DatePipe('de-AT').transform(new Date(2026, 0, 6), 'd. MMMM y')).toBe(
      '6. Jänner 2026',
    );
  });

  it.each(['4.11.2026', '04.11.2026'])('parses %s as 4 November 2026', (input) => {
    const date = adapter.parse(input, formats.parse.dateInput);

    expect(date).toEqual(new Date(2026, 10, 4));
  });

  it('rejects input that is not a date', () => {
    const date = adapter.parse('31.02.2026', formats.parse.dateInput);

    expect(adapter.isValid(date as Date)).toBe(false);
  });

  it('displays dates as dd.MM.yyyy', () => {
    expect(adapter.format(new Date(2026, 10, 4), formats.display.dateInput)).toBe('04.11.2026');
  });

  it('starts the week on Monday', () => {
    expect(adapter.getFirstDayOfWeek()).toBe(1);
  });
});
