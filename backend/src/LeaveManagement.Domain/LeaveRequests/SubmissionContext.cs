using LeaveManagement.Domain.Entitlements;
using LeaveManagement.Domain.Holidays;

namespace LeaveManagement.Domain.LeaveRequests;

/// <summary>
/// Everything the business rules need to know when a request is submitted. The caller loads it,
/// so the domain stays free of data access.
/// </summary>
/// <param name="Calendar">Holidays covering the requested period.</param>
/// <param name="ExistingRequests">All requests of the employee; only pending and approved ones count.</param>
/// <param name="Entitlements">All leave entitlements of the employee.</param>
/// <param name="Today">The current date in the company's time zone.</param>
/// <param name="Now">The current point in time, stored as the submission time.</param>
public sealed record SubmissionContext(
    WorkingDayCalendar Calendar,
    IReadOnlyCollection<LeaveRequest> ExistingRequests,
    IReadOnlyCollection<LeaveEntitlement> Entitlements,
    DateOnly Today,
    DateTimeOffset Now);
