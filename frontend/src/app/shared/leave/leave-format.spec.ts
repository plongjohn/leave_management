import { formatDayCount, formatPeriod } from './leave-format';

describe('formatPeriod', () => {
  it.each([
    ['2026-06-19', '2026-06-19', '19.06.2026'],
    ['2026-11-16', '2026-11-20', '16.11. – 20.11.2026'],
    ['2026-12-28', '2027-01-04', '28.12.2026 – 04.01.2027'],
  ])('formats %s to %s as "%s"', (start, end, expected) => {
    expect(formatPeriod(start, end)).toBe(expected);
  });
});

describe('formatDayCount', () => {
  it.each([
    [1, '1 Arbeitstag'],
    [5, '5 Arbeitstage'],
    [7.5, '7,5 Arbeitstage'],
    [0, '0 Arbeitstage'],
    [-2, '-2 Arbeitstage'],
  ])('formats %d as "%s"', (days, expected) => {
    expect(formatDayCount(days, 'Arbeitstag', 'Arbeitstage')).toBe(expected);
  });
});
