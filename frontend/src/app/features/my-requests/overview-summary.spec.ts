import { LeaveBalance, MyLeaveRequest } from './my-requests.models';
import { summarizeOverview } from './overview-summary';

describe('summarizeOverview', () => {
  const today = '2026-11-03';

  const balance: LeaveBalance = {
    availableDays: 8,
    years: [
      {
        year: 2025,
        entitledDays: 25,
        approvedDays: 25,
        pendingDays: 0,
        remainingDays: 0,
        expiresOn: '2027-12-31',
      },
      {
        year: 2026,
        entitledDays: 25,
        approvedDays: 10,
        pendingDays: 7,
        remainingDays: 8,
        expiresOn: '2028-12-31',
      },
    ],
  };

  function request(overrides: Partial<MyLeaveRequest>): MyLeaveRequest {
    return {
      id: 'r',
      startDate: '2026-11-16',
      endDate: '2026-11-20',
      type: 'Vacation',
      specialLeaveReason: null,
      workingDays: 5,
      returnedDays: 0,
      status: 'Approved',
      comment: null,
      hasMedicalCertificate: false,
      isCertificateRevoked: false,
      createdAt: '2026-10-01T08:00:00+02:00',
      approverName: null,
      decidedByName: 'Andrea Steiner',
      decidedAt: '2026-10-02T08:00:00+02:00',
      canBeCancelled: true,
      version: 1,
      ...overrides,
    };
  }

  it('takes the available days and adds up the pending days of all years', () => {
    const summary = summarizeOverview(balance, [], today);

    expect(summary.availableDays).toBe(8);
    expect(summary.pendingVacationDays).toBe(7);
    expect(summary.nextAbsence).toBeNull();
  });

  it('counts pending requests and names each approver once, HR as null', () => {
    const summary = summarizeOverview(
      balance,
      [
        request({ id: 'a', status: 'Pending', approverName: 'Andrea Steiner' }),
        request({ id: 'b', status: 'Pending', approverName: 'Andrea Steiner' }),
        request({ id: 'c', status: 'Pending', approverName: null }),
        request({ id: 'd', status: 'Approved' }),
      ],
      today,
    );

    expect(summary.pendingRequests).toBe(3);
    expect(summary.approvers).toEqual(['Andrea Steiner', null]);
  });

  it('picks the earliest pending or approved absence that has not ended', () => {
    const summary = summarizeOverview(
      balance,
      [
        request({ id: 'later', startDate: '2026-12-21', endDate: '2026-12-31', status: 'Pending' }),
        request({ id: 'next', startDate: '2026-11-16', endDate: '2026-11-20' }),
        request({ id: 'over', startDate: '2026-08-03', endDate: '2026-08-07' }),
        request({
          id: 'rejected',
          startDate: '2026-11-05',
          endDate: '2026-11-05',
          status: 'Rejected',
        }),
        request({
          id: 'cancelled',
          startDate: '2026-11-04',
          endDate: '2026-11-04',
          status: 'Cancelled',
        }),
      ],
      today,
    );

    expect(summary.nextAbsence?.request.id).toBe('next');
    expect(summary.nextAbsence?.daysUntilStart).toBe(13);
  });

  it('reports an absence that is running today with a negative distance', () => {
    const summary = summarizeOverview(
      balance,
      [request({ type: 'SickLeave', startDate: '2026-11-02', endDate: '2026-11-04' })],
      today,
    );

    expect(summary.nextAbsence?.daysUntilStart).toBe(-1);
  });
});
