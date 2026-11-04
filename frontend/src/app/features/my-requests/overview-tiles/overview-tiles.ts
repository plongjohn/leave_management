import { DecimalPipe } from '@angular/common';
import { Component, computed, input } from '@angular/core';
import { formatDayCount, formatPeriod } from '../../../shared/leave/leave-format';
import { leaveTypeLabels } from '../../../shared/leave/leave.texts';
import { overviewTexts } from '../my-requests.texts';
import { NextAbsence, OverviewSummary } from '../overview-summary';

const texts = overviewTexts.tiles;

/** The key figures on top of the overview (presentational). */
@Component({
  selector: 'lm-overview-tiles',
  imports: [DecimalPipe],
  templateUrl: './overview-tiles.html',
  styleUrl: './overview-tiles.scss',
})
export class OverviewTiles {
  readonly summary = input.required<OverviewSummary>();

  protected readonly texts = texts;

  protected readonly availableUnit = computed(() =>
    this.summary().availableDays === 1 ? texts.day : texts.days,
  );

  protected readonly availableHint = computed(() => {
    const { availableDays, pendingVacationDays } = this.summary();
    if (availableDays < 0) {
      return texts.overdrawn;
    }
    return pendingVacationDays > 0
      ? texts.afterPending(formatDayCount(pendingVacationDays, texts.pendingDay, texts.pendingDays))
      : texts.nothingPending;
  });

  protected readonly openRequestsHint = computed(() => {
    const { pendingRequests, approvers } = this.summary();
    if (pendingRequests === 0) {
      return texts.noOpenRequests;
    }
    if (approvers.length > 1) {
      return texts.waitingForSeveral;
    }
    return approvers[0] ? texts.waitingFor(approvers[0]) : texts.waitingForHr;
  });

  protected readonly nextAbsence = computed(() => {
    const next = this.summary().nextAbsence;
    return (
      next && {
        period: formatPeriod(next.request.startDate, next.request.endDate),
        hint: hint(next),
      }
    );
  });
}

/** "Urlaub (beantragt) · 5 Arbeitstage · in 13 Tagen" */
function hint({ request, daysUntilStart }: NextAbsence): string {
  const type = leaveTypeLabels[request.type];
  return [
    request.status === 'Pending' ? `${type} (${texts.requested})` : type,
    formatDayCount(request.workingDays, texts.workingDay, texts.workingDays),
    relativeStart(daysUntilStart),
  ].join(' · ');
}

function relativeStart(daysUntilStart: number): string {
  if (daysUntilStart < 0) {
    return texts.running;
  }
  if (daysUntilStart === 0) {
    return texts.startsToday;
  }
  return daysUntilStart === 1 ? texts.startsTomorrow : texts.startsIn(daysUntilStart);
}
