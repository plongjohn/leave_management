import { LeaveType } from '../../shared/leave/leave.models';
import { CONCURRENCY_CONFLICT } from '../../core/http/api-error';

export const overviewTexts = {
  pageTitle: 'Übersicht – Urlaubsverwaltung',
  greeting: 'Hallo',
  loading: 'Übersicht wird geladen …',
  loadError: 'Die Übersicht konnte nicht geladen werden.',
  tiles: {
    available: 'Urlaub verfügbar',
    day: 'Tag',
    days: 'Tage',
    afterPending: (days: string) => `nach Abzug von ${days}`,
    pendingDay: 'beantragtem Tag',
    pendingDays: 'beantragten Tagen',
    nothingPending: 'keine Tage beantragt',
    overdrawn: 'mehr Urlaub verplant als vorhanden',
    openRequests: 'Offene Anträge',
    noOpenRequests: 'nichts offen',
    waitingFor: (name: string) => `wartet auf ${name}`,
    waitingForHr: 'wartet auf die Personalabteilung',
    waitingForSeveral: 'warten auf Entscheidung',
    nextAbsence: 'Nächste Abwesenheit',
    noNextAbsence: 'keine geplant',
    requested: 'beantragt',
    workingDay: 'Arbeitstag',
    workingDays: 'Arbeitstage',
    startsToday: 'ab heute',
    startsTomorrow: 'morgen',
    startsIn: (days: number) => `in ${days} Tagen`,
    running: 'läuft gerade',
  },
} as const;

export const requestTableTexts = {
  title: 'Meine Anträge',
  yearFilter: 'Jahr',
  loading: 'Anträge werden geladen …',
  loadError: 'Die Anträge konnten nicht geladen werden.',
  empty: (year: number) => `Keine Anträge in ${year}.`,
  columns: {
    period: 'Zeitraum',
    type: 'Art',
    workingDays: 'Arbeitstage',
    status: 'Status',
    actions: 'Aktion',
  },
  cancel: 'Stornieren',
  cancelLabel: (period: string) => `Antrag ${period} stornieren`,
  returnedDays: (days: string) => `davon ${days} wegen Krankheit zurück`,
  day: 'Tag',
  days: 'Tage',
  withCertificate: 'mit ärztlicher Bestätigung',
  certificateRevoked: 'Bestätigung nicht anerkannt',
} as const;

export const balanceCardTexts = {
  title: 'Urlaubsanspruch',
  subtitle: 'Tage werden immer vom ältesten Anspruch genommen.',
  empty: 'Für dich ist noch kein Urlaubsanspruch angelegt.',
  year: (year: number) => `Anspruch ${year}`,
  expires: 'verfällt',
  entitled: 'Anspruch',
  approved: 'genommen',
  pending: 'beantragt',
  remaining: 'übrig',
  barLabel: (year: number, approved: string, pending: string, remaining: string) =>
    `${year}: ${approved} genommen, ${pending} beantragt, ${remaining} übrig`,
} as const;

export const requestDialogTexts = {
  open: 'Abwesenheit eintragen',
  title: 'Abwesenheit eintragen',
  subtitle: 'Wähle die Art und den Zeitraum.',
  close: 'Schließen',
  typeLabel: 'Art der Abwesenheit',
  reasonLabel: 'Anlass',
  reasonRequired: 'Bitte wähle den Anlass.',
  reasonsLoadError: 'Die Anlässe konnten nicht geladen werden.',
  reasonOption: (label: string, days: string, perWorkingYear: boolean) =>
    `${label} (max. ${days}${perWorkingYear ? ' pro Arbeitsjahr' : ''})`,
  workingDay: 'Arbeitstag',
  workingDays: 'Arbeitstage',
  day: 'Tag',
  days: 'Tage',
  periodLabel: 'Zeitraum',
  start: 'Von',
  end: 'Bis',
  periodHint: 'Wochenenden und Feiertage sind im Kalender markiert.',
  periodRequired: 'Bitte gib Beginn und Ende an (TT.MM.JJJJ).',
  periodInvalid: 'Bitte prüfe den Zeitraum.',
  periodOrder: 'Das Ende darf nicht vor dem Beginn liegen.',
  periodInPast: 'Nur eine Krankmeldung darf in der Vergangenheit beginnen.',
  commentTooLong: (max: number) => `Höchstens ${max} Zeichen.`,
  commentLabel: 'Kommentar (optional)',
  commentPlaceholder: {
    Vacation: 'z. B. Weihnachtsurlaub',
    TimeOffInLieu: 'z. B. Ausgleich für Release-Wochenende',
    SickLeave: 'Optionaler Hinweis für die Personalabteilung',
    SpecialLeave: 'z. B. neue Adresse',
  } satisfies Record<LeaveType, string>,
  sickLeaveInfo: 'Krankmeldungen gelten sofort, eine Genehmigung ist nicht nötig.',
  cancel: 'Abbrechen',
  submit: 'Antrag senden',
  submitSickLeave: 'Krankmeldung senden',
  submitted: 'Dein Antrag wurde gesendet.',
  sickLeaveReported: 'Deine Krankmeldung wurde gespeichert.',
} as const;

export const requestPreviewTexts = {
  loading: 'Antrag wird geprüft …',
  unavailable: 'Die Vorschau ist gerade nicht verfügbar. Du kannst trotzdem senden.',
  workingDay: 'Arbeitstag',
  workingDays: 'Arbeitstage',
  availableAfter: (days: string) => `danach ${days} verfügbar`,
  day: 'Tag',
  days: 'Tage',
  noVacationDeduction: 'kein Urlaubsabzug',
  returnedDays: (days: string) => `${days} Urlaub bekommst du zurück`,
  notCounted: (days: string) => `Nicht gezählt: ${days}.`,
  // The dates already end with a dot ("24.12. und 31.12.").
  halfDays: (days: string) => `Halbe Arbeitstage: ${days}`,
  absentColleagues: (names: string, count: number) =>
    `${names} ${count === 1 ? 'ist' : 'sind'} in diesem Zeitraum ebenfalls abwesend.`,
  approver: (name: string) => `${name} entscheidet über deinen Antrag.`,
  approverHr: 'Die Personalabteilung entscheidet über deinen Antrag.',
} as const;

export const certificateTexts = {
  label: 'Ärztliche Bestätigung',
  optional: '(optional)',
  choose: 'Datei hierher ziehen oder',
  chooseLink: 'auswählen',
  hint: 'PDF, JPG oder PNG, max. 5 MB · nur für dich und die Personalabteilung sichtbar',
  remove: (name: string) => `${name} entfernen`,
  required: 'Bitte lade die ärztliche Bestätigung hoch.',
  wrongType: 'Bitte wähle eine PDF-, JPG- oder PNG-Datei.',
  tooLarge: 'Die Datei darf höchstens 5 MB groß sein.',
  duringVacation: (returnedDays: string) =>
    `Die Krankmeldung überschneidet sich mit deinem genehmigten Urlaub. Jeder Krankheitstag zählt ` +
    `als Krankenstand: ${returnedDays} Urlaub bekommst du zurück. Dafür ist eine ärztliche ` +
    `Bestätigung Pflicht.`,
} as const;

export const cancelTexts = {
  title: 'Antrag stornieren?',
  message: (type: string, period: string) => `${type} ${period} wird storniert.`,
  approvedHint: 'Die Tage bekommst du wieder gutgeschrieben.',
  confirm: 'Stornieren',
  dismiss: 'Behalten',
  done: 'Der Antrag wurde storniert.',
  close: 'OK',
} as const;

/** German texts for the error codes this feature can receive (ADR 0008). */
export const myRequestErrorTexts: Readonly<Record<string, string>> = {
  'leave_request.starts_in_past': 'Nur eine Krankmeldung darf in der Vergangenheit beginnen.',
  'leave_request.starts_before_entry_date': 'Der Zeitraum beginnt vor deinem Eintrittsdatum.',
  'leave_request.no_working_days': 'Im gewählten Zeitraum liegt kein Arbeitstag.',
  'leave_request.overlapping': 'Der Zeitraum überschneidet sich mit einem anderen Antrag.',
  'leave_request.overlaps_pending_vacation':
    'Für diesen Zeitraum ist noch ein Urlaubsantrag offen. Storniere ihn zuerst.',
  'leave_request.medical_certificate_required':
    'Für mehr als drei Krankheitstage während des Urlaubs ist eine ärztliche Bestätigung nötig.',
  'leave_request.insufficient_balance': 'Dafür reicht dein Urlaubsanspruch nicht aus.',
  'leave_request.special_leave_reason_required': 'Bitte wähle den Anlass für den Sonderurlaub.',
  'leave_request.special_leave_reason_not_allowed': 'Nur Sonderurlaub hat einen Anlass.',
  'leave_request.special_leave_limit_exceeded':
    'Für diesen Anlass stehen nicht so viele Tage Sonderurlaub zu.',
  'leave_request.already_started': 'Genehmigter Urlaub kann nur vor Beginn storniert werden.',
  'leave_request.not_cancellable': 'Dieser Antrag kann nicht mehr storniert werden.',
  [CONCURRENCY_CONFLICT]: 'Der Antrag wurde inzwischen geändert. Die Liste wurde neu geladen.',
};

export const unknownErrorText =
  'Das hat leider nicht geklappt. Bitte versuche es später noch einmal.';

/** The German text for an error code; unknown or missing codes get the general text. */
export function myRequestErrorText(code: string | null): string {
  return (code && myRequestErrorTexts[code]) || unknownErrorText;
}
