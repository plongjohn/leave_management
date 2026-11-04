import { Component, computed, input } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { format, parseISO } from 'date-fns';
import { APP_LOCALE } from '../../../core/i18n/locale.providers';
import { formatDayCount } from '../../../shared/leave/leave-format';
import { Holiday } from '../../../shared/leave/leave.models';
import { LeaveRequestPreview } from '../my-requests.models';
import { myRequestErrorText, requestPreviewTexts } from '../my-requests.texts';

const texts = requestPreviewTexts;
const names = new Intl.ListFormat(APP_LOCALE, { type: 'conjunction' });

/**
 * The effect of the request while the employee types: working days, balance afterwards, holidays,
 * absent colleagues and who decides. Everything comes from the backend's preview (ADR 0008).
 */
@Component({
  selector: 'lm-request-preview',
  imports: [MatIconModule, MatProgressBarModule],
  templateUrl: './request-preview.html',
  styleUrl: './request-preview.scss',
})
export class RequestPreview {
  readonly preview = input<LeaveRequestPreview>();
  readonly loading = input(false);
  readonly failed = input(false);

  protected readonly texts = texts;

  protected readonly workingDays = computed(() => {
    const preview = this.preview();
    return preview && formatDayCount(preview.workingDays, texts.workingDay, texts.workingDays);
  });

  /** Vacation: the balance afterwards; sick leave: returned vacation; otherwise no deduction. */
  protected readonly balance = computed(() => {
    const preview = this.preview();
    if (!preview) {
      return '';
    }
    if (preview.availableDaysAfter !== null) {
      return texts.availableAfter(
        formatDayCount(preview.availableDaysAfter, texts.day, texts.days),
      );
    }
    if (preview.returnedVacationDays > 0) {
      return texts.returnedDays(
        formatDayCount(preview.returnedVacationDays, texts.day, texts.days),
      );
    }
    return texts.noVacationDeduction;
  });

  /** Explains the count: which days are not counted at all, which only half. */
  protected readonly holidayNotes = computed(() => {
    const holidays = this.preview()?.holidaysInPeriod ?? [];
    const dayOf = (date: string) => format(parseISO(date), 'dd.MM.');
    const isHalfDay = (holiday: Holiday) => holiday.kind === 'CompanyHalfDay';
    const half = holidays.filter(isHalfDay);
    const free = holidays.filter((holiday) => !isHalfDay(holiday));
    return [
      free.length ? texts.notCounted(free.map((h) => `${dayOf(h.date)} ${h.name}`).join(', ')) : '',
      half.length ? texts.halfDays(names.format(half.map((h) => dayOf(h.date)))) : '',
    ].filter(Boolean);
  });

  protected readonly error = computed(() => {
    const code = this.preview()?.errorCode;
    return code ? myRequestErrorText(code) : null;
  });

  protected readonly colleagues = computed(() => {
    const colleagues = this.preview()?.absentColleagues ?? [];
    return colleagues.length
      ? {
          initials: colleagues.map((colleague) => initials(colleague.name)),
          text: texts.absentColleagues(
            names.format(colleagues.map((colleague) => colleague.name)),
            colleagues.length,
          ),
        }
      : null;
  });

  /** Sick leave needs no approval, so nobody is named. */
  protected readonly approver = computed(() => {
    const preview = this.preview();
    if (!preview?.needsApproval) {
      return null;
    }
    return preview.approverName ? texts.approver(preview.approverName) : texts.approverHr;
  });
}

function initials(name: string): string {
  return name
    .split(' ')
    .map((part) => part[0])
    .join('')
    .slice(0, 2)
    .toUpperCase();
}
