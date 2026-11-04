import { DatePipe } from '@angular/common';
import { Component, computed, inject } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { format } from 'date-fns';
import { forkJoin } from 'rxjs';
import { AuthService } from '../../../core/auth/auth.service';
import { MyRequestsService } from '../my-requests.service';
import { overviewTexts } from '../my-requests.texts';
import { summarizeOverview } from '../overview-summary';
import { OverviewTiles } from '../overview-tiles/overview-tiles';

/** Start page of every employee: key figures, own requests and balance. */
@Component({
  selector: 'lm-overview-page',
  imports: [DatePipe, MatProgressBarModule, OverviewTiles],
  templateUrl: './overview-page.html',
  styleUrl: './overview-page.scss',
})
export class OverviewPage {
  private readonly myRequests = inject(MyRequestsService);

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

  protected readonly summary = computed(() => {
    const overview = this.overview.value();
    return (
      overview &&
      summarizeOverview(
        overview.balance,
        [...overview.thisYear, ...overview.nextYear],
        format(this.now, 'yyyy-MM-dd'),
      )
    );
  });
}
