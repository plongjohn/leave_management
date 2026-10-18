using LeaveManagement.Domain.Common;
using LeaveManagement.Domain.Employees;
using LeaveManagement.Domain.LeaveRequests;

namespace LeaveManagement.Domain.Tests.TestData;

internal static class TestRequests
{
    /// <summary>Monday, 2 March 2026.</summary>
    public static readonly DateOnly Today = new(2026, 3, 2);

    public static readonly DateTimeOffset Now = new(2026, 3, 2, 8, 0, 0, TimeSpan.FromHours(1));

    public static LeaveRequest Submit(
        Employee employee,
        DateOnly start,
        DateOnly end,
        LeaveType type = LeaveType.Vacation,
        IReadOnlyCollection<LeaveRequest>? existingRequests = null,
        DateOnly? today = null,
        string? comment = null)
    {
        var draft = new LeaveRequestDraft(new DateRange(start, end), type, comment);
        var context = new SubmissionContext(TestCalendars.Austria, existingRequests ?? [], today ?? Today, Now);

        return LeaveRequest.Submit(employee, draft, context);
    }
}
