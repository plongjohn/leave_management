import { Component, computed, input } from '@angular/core';
import { LeaveRequestStatus, LeaveType } from '../leave.models';
import { leaveStatusLabels, sickLeaveReportedLabel } from '../leave.texts';

/** The status of a leave request as a coloured label (not interactive, so no Material chip). */
@Component({
  selector: 'lm-leave-status-chip',
  template: '{{ label() }}',
  styleUrl: './leave-status-chip.scss',
  host: {
    '[class]': 'variant()',
  },
})
export class LeaveStatusChip {
  readonly status = input.required<LeaveRequestStatus>();
  readonly type = input.required<LeaveType>();

  /** Approved sick leave is shown as reported – it never waited for anyone's approval. */
  private readonly reportedSickLeave = computed(
    () => this.type() === 'SickLeave' && this.status() === 'Approved',
  );

  protected readonly label = computed(() =>
    this.reportedSickLeave() ? sickLeaveReportedLabel : leaveStatusLabels[this.status()],
  );

  protected readonly variant = computed(() =>
    this.reportedSickLeave() ? 'reported' : this.status().toLowerCase(),
  );
}
