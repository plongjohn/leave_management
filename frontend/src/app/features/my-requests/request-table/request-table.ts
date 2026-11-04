import { DecimalPipe } from '@angular/common';
import { Component, computed, input, model, output } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatTableModule } from '@angular/material/table';
import { LeaveStatusChip } from '../../../shared/leave/leave-status-chip/leave-status-chip';
import { formatDayCount, formatPeriod } from '../../../shared/leave/leave-format';
import { leaveTypeLabels, specialLeaveReasonLabels } from '../../../shared/leave/leave.texts';
import { MyLeaveRequest } from '../my-requests.models';
import { requestTableTexts } from '../my-requests.texts';

const texts = requestTableTexts;

/** A request prepared for display. */
interface RequestRow {
  request: MyLeaveRequest;
  period: string;
  type: string;
  /** Second line under the type, e.g. the special leave reason; empty if there is nothing to say. */
  note: string;
}

/** "Meine Anträge": the requests of one year with year filter and cancel button (presentational). */
@Component({
  selector: 'lm-request-table',
  imports: [
    DecimalPipe,
    LeaveStatusChip,
    MatButtonModule,
    MatButtonToggleModule,
    MatProgressBarModule,
    MatTableModule,
  ],
  templateUrl: './request-table.html',
  styleUrl: './request-table.scss',
})
export class RequestTable {
  /** The selected year; two-way bound, the page loads the requests for it. */
  readonly year = model.required<number>();
  readonly years = input.required<readonly number[]>();
  readonly requests = input<readonly MyLeaveRequest[]>();
  readonly loading = input(false);
  readonly failed = input(false);
  /** The request being cancelled right now; its button is disabled. */
  readonly cancellingId = input<string | null>(null);

  readonly cancelRequest = output<MyLeaveRequest>();

  protected readonly texts = texts;
  protected readonly columns = ['period', 'type', 'workingDays', 'status', 'actions'];

  protected readonly rows = computed(() => (this.requests() ?? []).map(toRow));
}

function toRow(request: MyLeaveRequest): RequestRow {
  return {
    request,
    period: formatPeriod(request.startDate, request.endDate),
    type: leaveTypeLabels[request.type],
    note: notes(request).join(' · '),
  };
}

function notes(request: MyLeaveRequest): string[] {
  const notes: string[] = [];
  if (request.specialLeaveReason) {
    notes.push(specialLeaveReasonLabels[request.specialLeaveReason]);
  }
  if (request.returnedDays > 0) {
    notes.push(texts.returnedDays(formatDayCount(request.returnedDays, texts.day, texts.days)));
  }
  if (request.isCertificateRevoked) {
    notes.push(texts.certificateRevoked);
  } else if (request.hasMedicalCertificate) {
    notes.push(texts.withCertificate);
  }
  return notes;
}
