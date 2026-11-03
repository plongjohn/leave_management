using LeaveManagement.Domain.Common;

namespace LeaveManagement.Domain.LeaveRequests;

/// <summary>
/// Sick leave during approved vacation (§ 5 UrlG, ADR 0007): days of sickness do not count as leave,
/// so the vacation days are returned. The company returns every sick day, not only from the fourth
/// day on as the law requires (decision 2026-11-03).
/// </summary>
public static class SickLeaveDuringVacation
{
    /// <summary>
    /// The first calendar days of a sickness need no medical certificate, as is common under Austrian
    /// labour law. A longer sick leave during vacation must come with a certificate.
    /// </summary>
    public const int CalendarDaysWithoutCertificate = 3;

    public static bool RequiresCertificate(DateRange sickPeriod) =>
        sickPeriod.CalendarDays > CalendarDaysWithoutCertificate;

    /// <summary>Sick leave may overlap approved vacation only; any other active request blocks it.</summary>
    internal static bool MayOverlap(LeaveRequest request) =>
        request.Type == LeaveType.Vacation && request.Status == LeaveRequestStatus.Approved;
}
