import { differenceInCalendarDays, parseISO } from 'date-fns';
import { LeaveBalance, MyLeaveRequest } from './my-requests.models';

/** What the tiles on top of the overview show. */
export interface OverviewSummary {
  /** May be negative (ADR 0007 addendum). */
  availableDays: number;
  /** Vacation days reserved by pending requests, over all entitlement years. */
  pendingVacationDays: number;
  pendingRequests: number;
  /**
   * Who the pending requests wait for, without duplicates; `null` stands for HR. Several entries
   * only if the manager changed while a request was pending.
   */
  approvers: (string | null)[];
  nextAbsence: NextAbsence | null;
}

export interface NextAbsence {
  request: MyLeaveRequest;
  /** 0 = starts today, negative = has already started. */
  daysUntilStart: number;
}

/**
 * Derives the tiles from the balance and the upcoming requests.
 *
 * @param today ISO date (yyyy-MM-dd); ISO dates compare correctly as strings.
 */
export function summarizeOverview(
  balance: LeaveBalance,
  requests: readonly MyLeaveRequest[],
  today: string,
): OverviewSummary {
  const pending = requests.filter((request) => request.status === 'Pending');

  const nextRequest = requests
    .filter((request) => request.status === 'Pending' || request.status === 'Approved')
    .filter((request) => request.endDate >= today)
    .reduce<MyLeaveRequest | null>(
      (earliest, request) =>
        !earliest || request.startDate < earliest.startDate ? request : earliest,
      null,
    );

  return {
    availableDays: balance.availableDays,
    pendingVacationDays: balance.years.reduce((sum, year) => sum + year.pendingDays, 0),
    pendingRequests: pending.length,
    approvers: [...new Set(pending.map((request) => request.approverName))],
    nextAbsence: nextRequest && {
      request: nextRequest,
      daysUntilStart: differenceInCalendarDays(parseISO(nextRequest.startDate), parseISO(today)),
    },
  };
}
