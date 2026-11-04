import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { Holiday, SpecialLeaveReasonLimit } from './leave.models';

/** Reference data every leave feature needs: holidays and special leave limits. */
@Injectable({ providedIn: 'root' })
export class LeaveDataService {
  private readonly http = inject(HttpClient);

  /** Statutory holidays and company days of one year, sorted by date. */
  getHolidays(year: number): Observable<Holiday[]> {
    const params = new HttpParams().set('year', year);
    return this.http.get<Holiday[]>('/api/holidays', { params });
  }

  /** Every special leave reason with its limit, in the order of the drop-down. */
  getSpecialLeaveReasons(): Observable<SpecialLeaveReasonLimit[]> {
    return this.http.get<SpecialLeaveReasonLimit[]>('/api/special-leave-reasons');
  }
}
