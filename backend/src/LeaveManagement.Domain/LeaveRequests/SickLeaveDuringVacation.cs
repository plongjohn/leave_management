namespace LeaveManagement.Domain.LeaveRequests;

/// <summary>
/// Sick leave during approved vacation (§ 5 UrlG, ADR 0007): days of sickness do not count as leave,
/// so the vacation days are returned. The company returns every sick day, not only from the fourth
/// day on as the law requires (decision 2026-11-03).
/// </summary>
public static class SickLeaveDuringVacation
{
    /// <summary>
    /// The first working days of a sickness need no medical certificate, as is common under Austrian
    /// labour law. Weekends and holidays do not count, because the employee would not work on them
    /// anyway (decision 2026-11-03). A longer sick leave during vacation must come with a certificate.
    /// </summary>
    public const int WorkingDaysWithoutCertificate = 3;

    /// <param name="sickDays">The working days of the sick leave.</param>
    public static bool RequiresCertificate(IReadOnlyCollection<LeaveRequestDay> sickDays)
    {
        ArgumentNullException.ThrowIfNull(sickDays);
        return sickDays.Count > WorkingDaysWithoutCertificate;
    }

    /// <summary>Sick leave may overlap approved vacation only; any other active request blocks it.</summary>
    internal static bool MayOverlap(LeaveRequest request) =>
        request.Type == LeaveType.Vacation && request.Status == LeaveRequestStatus.Approved;

    /// <summary>
    /// The working days of <paramref name="vacation"/> that count as sickness and are therefore not
    /// taken from the entitlement.
    /// </summary>
    /// <param name="requestsOfEmployee">All requests of the employee; only active sick leave counts.</param>
    public static decimal ReturnedDays(LeaveRequest vacation, IEnumerable<LeaveRequest> requestsOfEmployee)
    {
        ArgumentNullException.ThrowIfNull(vacation);

        var sickDates = SickDates(requestsOfEmployee.Where(request => request.EmployeeId == vacation.EmployeeId));
        return vacation.Days.Where(day => sickDates.Contains(day.Date)).Sum(day => day.WorkingDays);
    }

    /// <summary>
    /// The working days on which the employee counts as sick. Without a certificate only the first
    /// working days of a sickness count, which matters once HR has revoked the certificate; shorter
    /// sick leave never needs one.
    /// </summary>
    internal static HashSet<DateOnly> SickDates(IEnumerable<LeaveRequest> requests)
    {
        ArgumentNullException.ThrowIfNull(requests);

        return requests
            .Where(request => request.Type == LeaveType.SickLeave && request.IsActive)
            .SelectMany(request =>
            {
                var sickDates = request.Days.Select(day => day.Date).Order();
                return request.IsCertificateRevoked ? sickDates.Take(WorkingDaysWithoutCertificate) : sickDates;
            })
            .ToHashSet();
    }
}
