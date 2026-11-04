// Types shared by the leave features; they match the enums and records of the backend, which
// sends enums as strings and DateOnly as ISO dates (yyyy-MM-dd).

/** Matches LeaveType; the order is the order of the type tiles in the request dialog. */
export const LEAVE_TYPES = ['Vacation', 'TimeOffInLieu', 'SickLeave', 'SpecialLeave'] as const;
export type LeaveType = (typeof LEAVE_TYPES)[number];

/** Matches SpecialLeaveReason. */
export const SPECIAL_LEAVE_REASONS = [
  'OwnWedding',
  'BirthOfChild',
  'DeathOfCloseRelative',
  'DeathOfRelative',
  'WeddingOfChildOrSibling',
  'Relocation',
  'CareLeave',
] as const;
export type SpecialLeaveReason = (typeof SPECIAL_LEAVE_REASONS)[number];

/** Matches LeaveRequestStatus. */
export type LeaveRequestStatus = 'Pending' | 'Approved' | 'Rejected' | 'Cancelled';

/** Matches HolidayKind. */
export type HolidayKind = 'Statutory' | 'CompanyDayOff' | 'CompanyHalfDay';

/** Matches HolidayResponse. */
export interface Holiday {
  /** ISO date (yyyy-MM-dd). */
  date: string;
  name: string;
  kind: HolidayKind;
}
