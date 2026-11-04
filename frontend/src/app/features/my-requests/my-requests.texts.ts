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
