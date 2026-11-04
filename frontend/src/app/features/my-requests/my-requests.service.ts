import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import {
  LeaveBalance,
  LeaveRequestDraft,
  LeaveRequestPreview,
  MyLeaveRequest,
  SickLeaveReport,
} from './my-requests.models';

/** The signed-in employee's own requests and balance (`/api/me`, ADR 0008). */
@Injectable({ providedIn: 'root' })
export class MyRequestsService {
  private readonly http = inject(HttpClient);

  getRequests(year: number): Observable<MyLeaveRequest[]> {
    const params = new HttpParams().set('year', year);
    return this.http.get<MyLeaveRequest[]>('/api/me/leave-requests', { params });
  }

  getBalance(): Observable<LeaveBalance> {
    return this.http.get<LeaveBalance>('/api/me/leave-balance');
  }

  /** Runs the submission rules without saving; a violated rule comes back as `errorCode`. */
  preview(draft: LeaveRequestDraft): Observable<LeaveRequestPreview> {
    return this.http.post<LeaveRequestPreview>('/api/me/leave-requests/preview', draft);
  }

  /** For every type except sick leave, which is reported with `reportSickLeave`. */
  submit(draft: LeaveRequestDraft): Observable<MyLeaveRequest> {
    return this.http.post<MyLeaveRequest>('/api/me/leave-requests', draft);
  }

  /** Report and certificate in one multipart request, so they are saved together. */
  reportSickLeave(report: SickLeaveReport): Observable<MyLeaveRequest> {
    const form = new FormData();
    form.append('startDate', report.startDate);
    form.append('endDate', report.endDate);
    if (report.comment) {
      form.append('comment', report.comment);
    }
    if (report.certificate) {
      form.append('certificate', report.certificate, report.certificate.name);
    }

    // No Content-Type header: the browser sets multipart/form-data with the boundary itself.
    return this.http.post<MyLeaveRequest>('/api/me/sick-leaves', form);
  }

  /** Sends the version the user saw; a change in the meantime answers 409 (ADR 0008). */
  cancel(request: Pick<MyLeaveRequest, 'id' | 'version'>): Observable<void> {
    return this.http.post<void>(`/api/me/leave-requests/${request.id}/cancel`, {
      version: request.version,
    });
  }
}
