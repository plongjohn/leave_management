import {
  Holiday,
  LeaveRequestStatus,
  LeaveType,
  SpecialLeaveReason,
} from '../../shared/leave/leave.models';

// Dates are ISO dates (yyyy-MM-dd), timestamps ISO strings with offset, as the backend sends them.

/** Matches MyLeaveRequestResponse. */
export interface MyLeaveRequest {
  id: string;
  startDate: string;
  endDate: string;
  type: LeaveType;
  specialLeaveReason: SpecialLeaveReason | null;
  workingDays: number;
  /** Vacation only: working days that count as sickness and are not taken. */
  returnedDays: number;
  status: LeaveRequestStatus;
  comment: string | null;
  hasMedicalCertificate: boolean;
  isCertificateRevoked: boolean;
  createdAt: string;
  /** Pending only: the manager who decides; `null` means HR decides. */
  approverName: string | null;
  decidedByName: string | null;
  decidedAt: string | null;
  canBeCancelled: boolean;
  /** Concurrency token; sent back when cancelling. */
  version: number;
}

/** Matches LeaveBalanceResponse. */
export interface LeaveBalance {
  /** Entitlements that have not expired yet, oldest first. */
  years: EntitlementYear[];
  /** May be negative, e.g. after HR revoked a medical certificate. */
  availableDays: number;
}

/** Matches EntitlementYearResponse. */
export interface EntitlementYear {
  year: number;
  entitledDays: number;
  approvedDays: number;
  pendingDays: number;
  remainingDays: number;
  expiresOn: string;
}

/** Matches LeaveRequestDraftRequest: what the employee enters in the request dialog. */
export interface LeaveRequestDraft {
  startDate: string;
  endDate: string;
  type: LeaveType;
  comment?: string | null;
  specialLeaveReason?: SpecialLeaveReason | null;
  /** Sick leave only: a certificate will be uploaded with the report. */
  hasMedicalCertificate?: boolean;
}

/** Matches LeaveRequestPreviewResponse. */
export interface LeaveRequestPreview {
  workingDays: number;
  holidaysInPeriod: Holiday[];
  /** Vacation only. */
  availableDaysAfter: number | null;
  /** Sick leave only: approved vacation days that would come back. */
  returnedVacationDays: number;
  /** The rule the request would violate, `null` if it can be submitted. */
  errorCode: string | null;
  needsApproval: boolean;
  /** `null` when HR decides or no approval is needed. */
  approverName: string | null;
  absentColleagues: AbsentColleague[];
}

/** Matches AbsentColleagueResponse. */
export interface AbsentColleague {
  employeeId: string;
  name: string;
}

/** The fields of the multipart form of POST /api/me/sick-leaves. */
export interface SickLeaveReport {
  startDate: string;
  endDate: string;
  comment?: string | null;
  certificate?: File | null;
}
