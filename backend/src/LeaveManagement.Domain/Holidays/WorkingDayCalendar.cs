using LeaveManagement.Domain.Common;

namespace LeaveManagement.Domain.Holidays;

/// <summary>
/// Knows how much of a working day each calendar day is: Monday to Friday count as one day,
/// weekends and holidays as none, company half days as half a day.
/// Part-time and other working-week models are not supported yet (ADR 0007).
/// </summary>
public sealed class WorkingDayCalendar
{
    private const decimal FullDay = 1m;
    private const decimal HalfDay = 0.5m;
    private const decimal NoWorkingDay = 0m;

    private readonly Dictionary<DateOnly, HolidayKind> _holidays;

    public WorkingDayCalendar(IEnumerable<Holiday> holidays)
    {
        ArgumentNullException.ThrowIfNull(holidays);
        _holidays = holidays.ToDictionary(holiday => holiday.Date, holiday => holiday.Kind);
    }

    public decimal WorkingDaysOn(DateOnly date)
    {
        if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
        {
            return NoWorkingDay;
        }

        if (!_holidays.TryGetValue(date, out var kind))
        {
            return FullDay;
        }

        return kind == HolidayKind.CompanyHalfDay ? HalfDay : NoWorkingDay;
    }

    public decimal CountWorkingDays(DateRange period) => period.Days().Sum(WorkingDaysOn);
}
