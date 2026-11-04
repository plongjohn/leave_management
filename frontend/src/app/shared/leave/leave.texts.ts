import { HolidayKind, LeaveRequestStatus, LeaveType, SpecialLeaveReason } from './leave.models';

export const leaveTypeLabels = {
  Vacation: 'Urlaub',
  TimeOffInLieu: 'Zeitausgleich',
  SickLeave: 'Krankmeldung',
  SpecialLeave: 'Sonderurlaub',
} as const satisfies Record<LeaveType, string>;

export const leaveStatusLabels = {
  Pending: 'Offen',
  Approved: 'Genehmigt',
  Rejected: 'Abgelehnt',
  Cancelled: 'Storniert',
} as const satisfies Record<LeaveRequestStatus, string>;

/** Sick leave needs no approval; "Genehmigt" would be misleading. */
export const sickLeaveReportedLabel = 'Gemeldet';

export const specialLeaveReasonLabels = {
  OwnWedding: 'Eigene Hochzeit',
  BirthOfChild: 'Geburt eines Kindes',
  DeathOfCloseRelative: 'Tod von Ehe- oder Lebenspartner, Kind oder Elternteil',
  DeathOfRelative: 'Tod von Geschwistern, Schwiegereltern oder Großeltern',
  WeddingOfChildOrSibling: 'Hochzeit eines Kindes oder Geschwisters',
  Relocation: 'Wohnungswechsel',
  CareLeave: 'Pflegefreistellung',
} as const satisfies Record<SpecialLeaveReason, string>;

export const holidayKindLabels = {
  Statutory: 'Feiertag',
  CompanyDayOff: 'Betrieblich frei',
  CompanyHalfDay: 'Halber Arbeitstag',
} as const satisfies Record<HolidayKind, string>;
