import { Holiday } from './leave.models';
import { leaveDayClasses } from './leave-calendar';

describe('leaveDayClasses', () => {
  const holidays: Holiday[] = [
    { date: '2026-12-08', name: 'Mariä Empfängnis', kind: 'Statutory' },
    { date: '2026-12-24', name: 'Heiliger Abend', kind: 'CompanyHalfDay' },
    { date: '2026-12-07', name: 'Fenstertag', kind: 'CompanyDayOff' },
    { date: '2026-12-26', name: 'Stefanitag', kind: 'Statutory' },
  ];
  const dayClass = leaveDayClasses(holidays);

  it.each([
    ['a working day', new Date(2026, 11, 9), ''],
    ['a Saturday', new Date(2026, 11, 12), 'lm-calendar-weekend'],
    ['a Sunday', new Date(2026, 11, 13), 'lm-calendar-weekend'],
    ['a statutory holiday', new Date(2026, 11, 8), 'lm-calendar-holiday'],
    ['a company day off', new Date(2026, 11, 7), 'lm-calendar-holiday'],
    ['a company half day', new Date(2026, 11, 24), 'lm-calendar-half-day'],
    ['a holiday on a Saturday', new Date(2026, 11, 26), 'lm-calendar-holiday'],
  ])('marks %s', (_, date, expected) => {
    expect(dayClass(date, 'month')).toBe(expected);
  });

  it('leaves the year views unmarked', () => {
    expect(dayClass(new Date(2026, 11, 8), 'year')).toBe('');
    expect(dayClass(new Date(2026, 11, 8), 'multi-year')).toBe('');
  });
});
