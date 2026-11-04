using LeaveManagement.Domain.Employees;

namespace LeaveManagement.Domain.LeaveRequests;

/// <summary>
/// Limits for special leave. The occasions follow § 8 Abs 3 AngG; the number of days is set by the
/// collective agreement and differs between industries. The values below are typical examples and
/// kept in code for the MVP (ADR 0007); an HR-maintained table may replace them later.
/// </summary>
public static class SpecialLeavePolicy
{
    private static readonly Dictionary<SpecialLeaveReason, SpecialLeaveLimit> Limits = new()
    {
        [SpecialLeaveReason.OwnWedding] = new(3m, SpecialLeaveLimitPeriod.Occasion),
        [SpecialLeaveReason.BirthOfChild] = new(1m, SpecialLeaveLimitPeriod.Occasion),
        [SpecialLeaveReason.DeathOfCloseRelative] = new(3m, SpecialLeaveLimitPeriod.Occasion),
        [SpecialLeaveReason.DeathOfRelative] = new(1m, SpecialLeaveLimitPeriod.Occasion),
        [SpecialLeaveReason.WeddingOfChildOrSibling] = new(1m, SpecialLeaveLimitPeriod.Occasion),
        [SpecialLeaveReason.Relocation] = new(2m, SpecialLeaveLimitPeriod.Occasion),

        // One working week per working year (§ 16 UrlG, five-day week).
        [SpecialLeaveReason.CareLeave] = new(5m, SpecialLeaveLimitPeriod.WorkingYear),
    };

    /// <summary>The limit of a reason; every reason has one.</summary>
    public static SpecialLeaveLimit LimitFor(SpecialLeaveReason reason) => Limits[reason];

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

        var limit = LimitFor(reason);

        if (limit.Period == SpecialLeaveLimitPeriod.Occasion)
        {
            if (newRequest.WorkingDays > limit.MaxWorkingDays)
            {
                throw LeaveRequestErrors.SpecialLeaveLimitExceededViolation(reason, limit.MaxWorkingDays);
            }

            return;
        }

        var daysPerWorkingYear = activeRequests
            .Where(request => request.SpecialLeaveReason == reason)
            .Append(newRequest)
            .SelectMany(request => request.Days)
            .GroupBy(day => WorkingYearStart(employee.EntryDate, day.Date));

        if (daysPerWorkingYear.Any(workingYear => workingYear.Sum(day => day.WorkingDays) > limit.MaxWorkingDays))
        {
            throw LeaveRequestErrors.SpecialLeaveLimitExceededViolation(reason, limit.MaxWorkingDays);
        }
    }
}
