import { DatePipe } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { MatDialog } from '@angular/material/dialog';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSnackBar } from '@angular/material/snack-bar';
import { format } from 'date-fns';
import { filter, finalize, forkJoin, switchMap, tap } from 'rxjs';
import { AuthService } from '../../../core/auth/auth.service';
import { apiErrorCode } from '../../../core/http/api-error';
import { openConfirmDialog } from '../../../shared/confirm-dialog/confirm-dialog';
import { formatPeriod } from '../../../shared/leave/leave-format';
import { leaveTypeLabels } from '../../../shared/leave/leave.texts';
import { MyLeaveRequest } from '../my-requests.models';
import { MyRequestsService } from '../my-requests.service';
import { cancelTexts, myRequestErrorText, overviewTexts } from '../my-requests.texts';
import { summarizeOverview } from '../overview-summary';
import { OverviewTiles } from '../overview-tiles/overview-tiles';
import { RequestTable } from '../request-table/request-table';

/** Start page of every employee: key figures, own requests and balance. */
@Component({
  selector: 'lm-overview-page',
  imports: [DatePipe, MatProgressBarModule, OverviewTiles, RequestTable],
  templateUrl: './overview-page.html',
  styleUrl: './overview-page.scss',
})
export class OverviewPage {
  private readonly myRequests = inject(MyRequestsService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);

  protected readonly texts = overviewTexts;
  protected readonly user = inject(AuthService).currentUser;
  protected readonly now = new Date();

  /**
   * The API returns requests per year; this and next year cover everything upcoming, e.g. a
   * pending vacation in January.
   */
  protected readonly overview = rxResource({
    stream: () =>
      forkJoin({
        balance: this.myRequests.getBalance(),
        thisYear: this.myRequests.getRequests(this.now.getFullYear()),
        nextYear: this.myRequests.getRequests(this.now.getFullYear() + 1),
      }),
  });

  // value() throws while a resource is in the error state, hence the hasValue() checks.
  protected readonly summary = computed(() => {
    if (!this.overview.hasValue()) {
      return undefined;
    }
    const { balance, thisYear, nextYear } = this.overview.value();
    return summarizeOverview(balance, [...thisYear, ...nextYear], format(this.now, 'yyyy-MM-dd'));
  });

  protected readonly years = [-1, 0, 1].map((offset) => this.now.getFullYear() + offset);
  protected readonly selectedYear = signal(this.now.getFullYear());

  /** Reloads by itself when another year is selected. */
  protected readonly requests = rxResource({
    params: () => this.selectedYear(),
    stream: ({ params: year }) => this.myRequests.getRequests(year),
  });

  protected readonly requestList = computed(() =>
    this.requests.hasValue() ? this.requests.value() : undefined,
  );

  protected readonly cancellingId = signal<string | null>(null);

  protected cancel(request: MyLeaveRequest): void {
    openConfirmDialog(this.dialog, {
      title: cancelTexts.title,
      message: cancelMessage(request),
      confirm: cancelTexts.confirm,
      dismiss: cancelTexts.dismiss,
    })
      .pipe(
        filter(Boolean),
        tap(() => this.cancellingId.set(request.id)),
        switchMap(() =>
          this.myRequests.cancel(request).pipe(finalize(() => this.cancellingId.set(null))),
        ),
      )
      .subscribe({
        next: () => {
          this.notify(cancelTexts.done);
          this.reload();
        },
        error: (error: unknown) => {
          const code = apiErrorCode(error);
          this.notify(myRequestErrorText(code));
          // A code means the request changed in the meantime (409) or no longer fits the rules
          // (422): show the current state. Without a code (e.g. offline) the list is still right.
          if (code) {
            this.reload();
          }
        },
      });
  }

  private reload(): void {
    this.overview.reload();
    this.requests.reload();
  }

  private notify(message: string): void {
    this.snackBar.open(message, cancelTexts.close, { duration: 6000 });
  }
}

function cancelMessage(request: MyLeaveRequest): string {
  const message = cancelTexts.message(
    leaveTypeLabels[request.type],
    formatPeriod(request.startDate, request.endDate),
  );
  return request.type === 'Vacation' ? `${message} ${cancelTexts.approvedHint}` : message;
}
