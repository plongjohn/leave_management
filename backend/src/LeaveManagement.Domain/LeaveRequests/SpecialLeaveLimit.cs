namespace LeaveManagement.Domain.LeaveRequests;

/// <summary>How many working days of special leave a reason allows, and for which period.</summary>
public sealed record SpecialLeaveLimit(decimal MaxWorkingDays, SpecialLeaveLimitPeriod Period);

public enum SpecialLeaveLimitPeriod
{
    /// <summary>For one occasion, e.g. one wedding; one request is meant for one occasion.</summary>
    Occasion,

    /// <summary>Added up over the working year, which starts on the anniversary of the entry date.</summary>
    WorkingYear,
}
