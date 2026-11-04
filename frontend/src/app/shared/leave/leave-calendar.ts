import { MatCalendarCellClassFunction } from '@angular/material/datepicker';
import { format, isWeekend } from 'date-fns';
import { Holiday } from './leave.models';

/** CSS classes for the day cells of a date picker; styled globally because the calendar is an overlay. */
export const CALENDAR_DAY_CLASSES = {
  weekend: 'lm-calendar-weekend',
  holiday: 'lm-calendar-holiday',
  halfDay: 'lm-calendar-half-day',
} as const;

/**
 * Marks weekends, holidays and company days off as days without work, and company half days
 * (24./31.12.) separately – so employees see in the picker which days will not be counted.
 * Only the month view is marked; year views show whole months.
 */
export function leaveDayClasses(holidays: readonly Holiday[]): MatCalendarCellClassFunction<Date> {
  const kinds = new Map(holidays.map((holiday) => [holiday.date, holiday.kind]));

  return (date, view) => {
    if (view !== 'month') {
      return '';
    }

    const kind = kinds.get(format(date, 'yyyy-MM-dd'));
    if (kind === 'CompanyHalfDay') {
      return CALENDAR_DAY_CLASSES.halfDay;
    }
    if (kind) {
      return CALENDAR_DAY_CLASSES.holiday;
    }
    return isWeekend(date) ? CALENDAR_DAY_CLASSES.weekend : '';
  };
}
