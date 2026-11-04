import { Component, InjectionToken, computed, inject, signal } from '@angular/core';
import { rxResource, toObservable, toSignal } from '@angular/core/rxjs-interop';
import {
  FormField,
  FormRoot,
  TreeValidationResult,
  form,
  hidden,
  maxLength,
  required,
  validate,
} from '@angular/forms/signals';
import { MatButtonModule } from '@angular/material/button';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatDialog, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { format, startOfToday } from 'date-fns';
import {
  Observable,
  debounce,
  distinctUntilChanged,
  firstValueFrom,
  forkJoin,
  map,
  timer,
} from 'rxjs';
import { GERMAN_DATEPICKER_INTL } from '../../../core/i18n/german-datepicker-intl';
import { apiErrorCode } from '../../../core/http/api-error';
import { formatDayCount } from '../../../shared/leave/leave-format';
import { leaveDayClasses } from '../../../shared/leave/leave-calendar';
import { LeaveDataService } from '../../../shared/leave/leave-data.service';
import { LEAVE_TYPES, LeaveType, SpecialLeaveReason } from '../../../shared/leave/leave.models';
import { leaveTypeLabels, specialLeaveReasonLabels } from '../../../shared/leave/leave.texts';
import { LeaveRequestDraft, MyLeaveRequest } from '../my-requests.models';
import { MyRequestsService } from '../my-requests.service';
import { myRequestErrorText, requestDialogTexts } from '../my-requests.texts';
import { RequestPreview } from '../request-preview/request-preview';

const texts = requestDialogTexts;

/** How long the dialog waits after the last change before it asks the backend for a preview. */
export const PREVIEW_DEBOUNCE_MS = new InjectionToken<number>('PREVIEW_DEBOUNCE_MS', {
  factory: () => 300,
});

/** Matches LeaveRequest.CommentMaxLength of the backend. */
const COMMENT_MAX_LENGTH = 500;

const TYPE_ICONS: Record<LeaveType, string> = {
  Vacation: 'beach_access',
  TimeOffInLieu: 'schedule',
  SickLeave: 'sick',
  SpecialLeave: 'redeem',
};

/** What the employee enters; the form is built around this model (Signal Forms, ADR 0009). */
interface RequestFormModel {
  type: LeaveType;
  specialLeaveReason: SpecialLeaveReason | null;
  start: Date | null;
  end: Date | null;
  comment: string;
}

/**
 * "Abwesenheit eintragen": type, reason for special leave, period and comment. Closes with the
 * created request. The checks here only help while typing; the backend decides (coding guidelines).
 */
@Component({
  selector: 'lm-request-dialog',
  imports: [
    FormField,
    FormRoot,
    MatButtonModule,
    MatDatepickerModule,
    MatDialogModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatSelectModule,
    RequestPreview,
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
  private readonly previewDebounceMs = inject(PREVIEW_DEBOUNCE_MS);

  protected readonly texts = texts;
  protected readonly types = LEAVE_TYPES.map((type) => ({
    type,
    label: leaveTypeLabels[type],
    icon: TYPE_ICONS[type],
  }));

  private readonly model = signal<RequestFormModel>({
    type: 'Vacation',
    specialLeaveReason: null,
    start: null,
    end: null,
    comment: '',
  });

  protected readonly requestForm = form(
    this.model,
    (path) => {
      // A hidden field does not count for validity, so "required" only applies to special leave.
      hidden(path.specialLeaveReason, {
        when: ({ valueOf }) => valueOf(path.type) !== 'SpecialLeave',
      });
      required(path.specialLeaveReason, { message: texts.reasonRequired });

      // The picker reports unreadable input as an empty date, which "required" catches.
      required(path.start, { message: texts.periodRequired });
      required(path.end, { message: texts.periodRequired });
      validate(path.start, ({ value, valueOf }) => {
        const start = value();
        return start && start < this.today && valueOf(path.type) !== 'SickLeave'
          ? { kind: 'in_past', message: texts.periodInPast }
          : undefined;
      });
      validate(path.end, ({ value, valueOf }) => {
        const start = valueOf(path.start);
        const end = value();
        return start && end && end < start
          ? { kind: 'end_before_start', message: texts.periodOrder }
          : undefined;
      });

      maxLength(path.comment, COMMENT_MAX_LENGTH, {
        message: texts.commentTooLong(COMMENT_MAX_LENGTH),
      });
    },
    { submission: { action: () => this.send() } },
  );

  protected readonly type = computed(() => this.model().type);

  /** Only sick leave may lie in the past; the calendar greys out earlier days for the rest. */
  protected readonly minDate = computed(() => (this.type() === 'SickLeave' ? null : this.today));

  /**
   * One message for the period; start and end are two fields of one form field. The date inputs
   * add their own errors (e.g. `matDatepickerMin`) without a text, so the first error with a text
   * from the schema above wins.
   */
  protected readonly periodError = computed(() => {
    const errors = [...this.requestForm.start().errors(), ...this.requestForm.end().errors()];
    return (
      errors.find((error) => error.message)?.message ?? (errors.length ? texts.periodInvalid : '')
    );
  });

  /** The rule the backend rejected the request with, as German text. */
  protected readonly serverError = computed(
    () =>
      this.requestForm()
        .errors()
        .find((error) => error.kind === 'server')?.message,
  );

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

  /**
   * The request as the preview endpoint expects it, or `undefined` while the period or the reason
   * is missing or the period is one the form already rejects. The comment is not part of it, so
   * typing a comment causes no request.
   */
  private readonly draft = computed<LeaveRequestDraft | undefined>(() => {
    const { type, specialLeaveReason, start, end } = this.model();
    if (
      !start ||
      !end ||
      end < start ||
      (start < this.today && type !== 'SickLeave') ||
      (type === 'SpecialLeave' && !specialLeaveReason)
    ) {
      return undefined;
    }
    return {
      startDate: toIsoDate(start),
      endDate: toIsoDate(end),
      type,
      specialLeaveReason: type === 'SpecialLeave' ? specialLeaveReason : null,
    };
  });

  /**
   * Waits until the employee stops changing the input, so not every click asks the backend; an
   * incomplete draft passes at once and hides the preview. Equal drafts (e.g. after a change of
   * the comment) are dropped.
   */
  private readonly settledDraft = toSignal(
    toObservable(this.draft).pipe(
      debounce((draft) => timer(draft ? this.previewDebounceMs : 0)),
      distinctUntilChanged((a, b) => JSON.stringify(a) === JSON.stringify(b)),
    ),
    { initialValue: undefined },
  );

  // The params read only the settled draft: the resource loads again whenever its params function
  // runs again, even if it returns the same object.
  private readonly preview = rxResource({
    params: () => this.settledDraft(),
    stream: ({ params: draft }) => this.myRequests.preview(draft),
  });

  protected readonly previewValue = computed(() =>
    this.preview.hasValue() ? this.preview.value() : undefined,
  );
  protected readonly previewLoading = computed(() => this.preview.isLoading());
  protected readonly previewFailed = computed(() => !!this.preview.error());

  /** The backend would reject the request; sending makes no sense until the input changes. */
  protected readonly blockedByRule = computed(() => !!this.previewValue()?.errorCode);

  /** Runs only when the form is valid; a rejection becomes a form error instead of an exception. */
  private async send(): Promise<TreeValidationResult> {
    try {
      this.dialogRef.close(await firstValueFrom(this.request()));
      return undefined;
    } catch (error) {
      return { kind: 'server', message: myRequestErrorText(apiErrorCode(error)) };
    }
  }

  /** Sick leave goes as multipart form to its own endpoint (ADR 0008). */
  private request(): Observable<MyLeaveRequest> {
    const { type, specialLeaveReason, start, end, comment } = this.model();
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
