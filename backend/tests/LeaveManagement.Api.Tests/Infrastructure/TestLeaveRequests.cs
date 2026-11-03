using LeaveManagement.Domain.Common;
using LeaveManagement.Domain.Employees;
using LeaveManagement.Domain.Entitlements;
using LeaveManagement.Domain.Holidays;
using LeaveManagement.Domain.LeaveRequests;

namespace LeaveManagement.Api.Tests.Infrastructure;

/// <summary>
/// Builds requests through the domain, like the API does, to arrange test data. Uses a calendar without
/// holidays, so every Monday to Friday is one working day.
/// </summary>
internal static class TestLeaveRequests
{
    public static DateOnly Today { get; } = DateOnly.FromDateTime(LeaveManagementApiFactory.DefaultCompanyNow.UtcDateTime);

    public static DateTimeOffset Now => LeaveManagementApiFactory.DefaultCompanyNow;

    /// <param name="entitlements">Defaults to the standard entitlement of the start year.</param>
    public static LeaveRequest Submit(
        Employee employee,
        DateOnly start,
        DateOnly end,
        LeaveType type = LeaveType.Vacation,
        IReadOnlyCollection<LeaveRequest>? existingRequests = null,
        IReadOnlyCollection<LeaveEntitlement>? entitlements = null,
        bool hasMedicalCertificate = false)
    {
        var draft = new LeaveRequestDraft(new DateRange(start, end), type, HasMedicalCertificate: hasMedicalCertificate);
        var context = new SubmissionContext(
            new WorkingDayCalendar([]),
            existingRequests ?? [],
            entitlements ?? [LeaveEntitlement.Create(employee.Id, start.Year)],
            Today,
            Now);

        return LeaveRequest.Submit(employee, draft, context);
    }

    public static LeaveRequest Approved(
        Employee manager,
        Employee employee,
        DateOnly start,
        DateOnly end,
        IReadOnlyCollection<LeaveEntitlement>? entitlements = null)
    {
        var request = Submit(employee, start, end, entitlements: entitlements);
        request.Approve(manager, employee, Now);
        return request;
    }
}
