using LeaveManagement.Domain.Common;
using LeaveManagement.Domain.Employees;
using LeaveManagement.Domain.Entitlements;
using LeaveManagement.Domain.LeaveRequests;

namespace LeaveManagement.Domain.Tests.TestData;

internal static class TestRequests
{
    /// <summary>Monday, 2 March 2026.</summary>
    public static readonly DateOnly Today = new(2026, 3, 2);

    public static readonly DateTimeOffset Now = new(2026, 3, 2, 8, 0, 0, TimeSpan.FromHours(1));

    /// <param name="entitlements">Defaults to the standard entitlement for 2024–2030.</param>
    public static LeaveRequest Submit(
        Employee employee,
        DateOnly start,
        DateOnly end,
        LeaveType type = LeaveType.Vacation,
        IReadOnlyCollection<LeaveRequest>? existingRequests = null,
        IReadOnlyCollection<LeaveEntitlement>? entitlements = null,
        DateOnly? today = null,
        string? comment = null)
    {
        var draft = new LeaveRequestDraft(new DateRange(start, end), type, comment);
        var context = new SubmissionContext(
            TestCalendars.Austria,
            existingRequests ?? [],
            entitlements ?? StandardEntitlements(employee),
            today ?? Today,
            Now);

        return LeaveRequest.Submit(employee, draft, context);
    }

    public static LeaveRequest Approved(Employee manager, Employee employee, DateOnly start, DateOnly end)
    {
        var request = Submit(employee, start, end);
        request.Approve(manager, employee, Now);
        return request;
    }

    public static List<LeaveEntitlement> StandardEntitlements(Employee employee) =>
        [.. Enumerable.Range(2024, 7).Select(year => LeaveEntitlement.Create(employee.Id, year))];
}
