namespace LeaveManagement.Api.Common;

/// <summary>
/// The current time as the company sees it. Timestamps are stored in UTC, but business rules such
/// as "leave must not start in the past" need today's date in Austria: shortly after midnight in
/// Vienna it is still the previous day in UTC.
/// </summary>
internal sealed class CompanyClock(TimeProvider timeProvider)
{
    // A constant, not a setting: the application serves exactly one company (decision 2026-11-03).
    public const string TimeZoneId = "Europe/Vienna";

    private static readonly TimeZoneInfo TimeZone = TimeZoneInfo.FindSystemTimeZoneById(TimeZoneId);

    public DateTimeOffset Now() => timeProvider.GetUtcNow();

    public DateOnly Today() => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(Now(), TimeZone).DateTime);
}
