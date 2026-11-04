import { Component, computed, inject, signal } from '@angular/core';
import { rxResource, takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { GERMAN_DATEPICKER_INTL } from '../../../core/i18n/german-datepicker-intl';
import { MatDialog, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { format, startOfToday } from 'date-fns';
import { Observable, finalize, forkJoin, map } from 'rxjs';
import { apiErrorCode } from '../../../core/http/api-error';
import { formatDayCount } from '../../../shared/leave/leave-format';
import { leaveDayClasses } from '../../../shared/leave/leave-calendar';
import { LeaveDataService } from '../../../shared/leave/leave-data.service';
import { LEAVE_TYPES, LeaveType, SpecialLeaveReason } from '../../../shared/leave/leave.models';
import { leaveTypeLabels, specialLeaveReasonLabels } from '../../../shared/leave/leave.texts';
import { MyLeaveRequest } from '../my-requests.models';
import { MyRequestsService } from '../my-requests.service';
import { myRequestErrorText, requestDialogTexts } from '../my-requests.texts';

const texts = requestDialogTexts;

/** Matches LeaveRequest.CommentMaxLength of the backend. */
const COMMENT_MAX_LENGTH = 500;

const TYPE_ICONS: Record<LeaveType, string> = {
  Vacation: 'beach_access',
  TimeOffInLieu: 'schedule',
  SickLeave: 'sick',
  SpecialLeave: 'redeem',
};

/**
 * "Abwesenheit eintragen": type, reason for special leave, period and comment. Closes with the
 * created request. The rules are checked by the backend only (coding guidelines).
 */
@Component({
  selector: 'lm-request-dialog',
  imports: [
    MatButtonModule,
    MatDatepickerModule,
    MatDialogModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatSelectModule,
    ReactiveFormsModule,
  ],
  templateUrl: './request-dialog.html',
  styleUrl: './request-dialog.scss',
  providers: [GERMAN_DATEPICKER_INTL],
})
export class RequestDialog {
  private readonly myRequests = inject(MyRequestsService);
  private readonly leaveData = inject(LeaveDataService);
  private readonly dialogRef = inject<MatDialogRef<RequestDialog, MyLeaveRequest>>(MatDialogRef);
  private readonly today = startOfToday();

  protected readonly texts = texts;
  protected readonly commentMaxLength = COMMENT_MAX_LENGTH;
  protected readonly types = LEAVE_TYPES.map((type) => ({
    type,
    label: leaveTypeLabels[type],
    icon: TYPE_ICONS[type],
  }));

  protected readonly form = inject(NonNullableFormBuilder).group({
    type: ['Vacation' as LeaveType],
    // Enabled for special leave only; a disabled control is neither validated nor sent.
    specialLeaveReason: [
      { value: null as SpecialLeaveReason | null, disabled: true },
      Validators.required,
    ],
    start: [null as Date | null, Validators.required],
    end: [null as Date | null, Validators.required],
    comment: ['', Validators.maxLength(COMMENT_MAX_LENGTH)],
  });

  protected readonly type = toSignal(this.form.controls.type.valueChanges, {
    initialValue: this.form.controls.type.value,
  });

  /** Only sick leave may lie in the past; the picker helps, the backend decides. */
  protected readonly minDate = computed(() => (this.type() === 'SickLeave' ? null : this.today));

  /** Last, this and next year: sick leave may lie in the past, vacation is planned ahead. */
  private readonly holidays = rxResource({
    stream: () => {
      const year = this.today.getFullYear();
      return forkJoin([year - 1, year, year + 1].map((y) => this.leaveData.getHolidays(y))).pipe(
        map((years) => years.flat()),
      );
    },
  });

  protected readonly dayClasses = computed(() =>
    leaveDayClasses(this.holidays.hasValue() ? this.holidays.value() : []),
  );

  private readonly reasons = rxResource({ stream: () => this.leaveData.getSpecialLeaveReasons() });

  protected readonly reasonOptions = computed(() =>
    (this.reasons.hasValue() ? this.reasons.value() : []).map((limit) => ({
      reason: limit.reason,
      label: texts.reasonOption(
        specialLeaveReasonLabels[limit.reason],
        formatDayCount(limit.maxWorkingDays, texts.workingDay, texts.workingDays),
        limit.period === 'WorkingYear',
      ),
    })),
  );

  protected readonly reasonsFailed = computed(() => !!this.reasons.error());

  protected readonly submitting = signal(false);
  protected readonly failure = signal<string | null>(null);

  constructor() {
    this.form.controls.type.valueChanges.pipe(takeUntilDestroyed()).subscribe((type) => {
      const reason = this.form.controls.specialLeaveReason;
      if (type === 'SpecialLeave') {
        reason.enable();
      } else {
        reason.reset(null);
        reason.disable();
      }
    });
  }

  /** One message for the period field; the date range input reports errors on both inputs. */
  protected periodError(): string {
    const { start, end } = this.form.controls;
    const errors = { ...start.errors, ...end.errors };
    if ('matDatepickerParse' in errors) {
      return texts.periodInvalid;
    }
    if ('matStartDateInvalid' in errors || 'matEndDateInvalid' in errors) {
      return texts.periodOrder;
    }
    if ('matDatepickerMin' in errors) {
      return texts.periodInPast;
    }
    return texts.periodRequired;
  }

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    this.failure.set(null);
    this.send()
      .pipe(finalize(() => this.submitting.set(false)))
      .subscribe({
        next: (created) => this.dialogRef.close(created),
        error: (error: unknown) => this.failure.set(myRequestErrorText(apiErrorCode(error))),
      });
  }

  /** Sick leave goes as multipart form to its own endpoint (ADR 0008). */
  private send(): Observable<MyLeaveRequest> {
    const { type, specialLeaveReason, start, end, comment } = this.form.getRawValue();
    const period = { startDate: toIsoDate(start!), endDate: toIsoDate(end!) };
    const trimmedComment = comment.trim() || null;

    if (type === 'SickLeave') {
      return this.myRequests.reportSickLeave({ ...period, comment: trimmedComment });
    }
    return this.myRequests.submit({
      ...period,
      type,
      specialLeaveReason: type === 'SpecialLeave' ? specialLeaveReason : null,
      comment: trimmedComment,
    });
  }
}

function toIsoDate(date: Date): string {
  return format(date, 'yyyy-MM-dd');
}

/** Opens the dialog; emits the created request, or nothing when the dialog was closed without. */
export function openRequestDialog(dialog: MatDialog): Observable<MyLeaveRequest | undefined> {
  return dialog
    .open<RequestDialog, void, MyLeaveRequest>(RequestDialog, {
      width: '560px',
      maxWidth: 'calc(100vw - 32px)',
      autoFocus: 'first-tabbable',
    })
    .afterClosed();
}
