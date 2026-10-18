using LeaveManagement.Domain.Employees;

namespace LeaveManagement.Domain.LeaveRequests;

/// <summary>
/// Limits for special leave. The occasions follow § 8 Abs 3 AngG; the number of days is set by the
/// collective agreement and differs between industries. The values below are typical examples and
/// kept in code for the MVP (ADR 0007); an HR-maintained table may replace them later.
/// </summary>
public static class SpecialLeavePolicy
{
    /// <summary>Care leave: one working week per working year (§ 16 UrlG, five-day week).</summary>
    public const decimal CareLeaveDaysPerWorkingYear = 5m;

    private static readonly Dictionary<SpecialLeaveReason, decimal> MaxDaysPerOccasion = new()
    {
        [SpecialLeaveReason.OwnWedding] = 3m,
        [SpecialLeaveReason.BirthOfChild] = 1m,
        [SpecialLeaveReason.DeathOfCloseRelative] = 3m,
        [SpecialLeaveReason.DeathOfRelative] = 1m,
        [SpecialLeaveReason.WeddingOfChildOrSibling] = 1m,
        [SpecialLeaveReason.Relocation] = 2m,
    };

    /// <summary>The maximum working days for one occasion; <c>null</c> for care leave, which is limited per working year.</summary>
    public static decimal? MaxWorkingDaysPerOccasion(SpecialLeaveReason reason) =>
        MaxDaysPerOccasion.TryGetValue(reason, out var days) ? days : null;

    /// <summary>The working year starts on the anniversary of the entry date.</summary>
    public static DateOnly WorkingYearStart(DateOnly entryDate, DateOnly date)
    {
        var anniversary = entryDate.AddYears(date.Year - entryDate.Year);
        return anniversary <= date ? anniversary : anniversary.AddYears(-1);
    }

    // One request is meant for one occasion; whether two requests concern the same occasion is
    // left to the approving manager.
    internal static void EnsureWithinLimits(
        LeaveRequest newRequest,
        Employee employee,
        IEnumerable<LeaveRequest> activeRequests)
    {
        var reason = newRequest.SpecialLeaveReason
            ?? throw new ArgumentException("Must be a special leave request.", nameof(newRequest));

        if (reason != SpecialLeaveReason.CareLeave)
        {
            var maxDays = MaxDaysPerOccasion[reason];
            if (newRequest.WorkingDays > maxDays)
            {
                throw LeaveRequestErrors.SpecialLeaveLimitExceededViolation(reason, maxDays);
            }

            return;
        }

        var careLeaveDays = activeRequests
            .Where(request => request.SpecialLeaveReason == SpecialLeaveReason.CareLeave)
            .Append(newRequest)
            .SelectMany(request => request.Days)
            .GroupBy(day => WorkingYearStart(employee.EntryDate, day.Date));

        if (careLeaveDays.Any(workingYear => workingYear.Sum(day => day.WorkingDays) > CareLeaveDaysPerWorkingYear))
        {
            throw LeaveRequestErrors.SpecialLeaveLimitExceededViolation(reason, CareLeaveDaysPerWorkingYear);
        }
    }
}
