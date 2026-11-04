namespace LeaveManagement.Infrastructure.Persistence.Configurations;

internal static class ColumnLengths
{
    /// <summary>Enums are stored as text so that the data stays readable; the longest name has 23 characters.</summary>
    public const int Enum = 32;

    /// <summary>Working days in half-day steps, up to 999.5 (numeric(4,1)).</summary>
    public const int DaysPrecision = 4;

    public const int DaysScale = 1;
}
