import { CONCURRENCY_CONFLICT } from '../../core/http/api-error';

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
