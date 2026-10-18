using LeaveManagement.Domain.Holidays;

namespace LeaveManagement.Domain.Tests.TestData;

internal static class TestCalendars
{
    /// <summary>Austrian public holidays 2024–2030 plus 24 and 31 December as company half days.</summary>
    public static WorkingDayCalendar Austria { get; } = new(
        Enumerable.Range(2024, 7).SelectMany(year => AustrianPublicHolidays.For(year).Concat(
        [
            Holiday.Create(new DateOnly(year, 12, 24), "Heiliger Abend", HolidayKind.CompanyHalfDay),
            Holiday.Create(new DateOnly(year, 12, 31), "Silvester", HolidayKind.CompanyHalfDay),
        ])));

    public static WorkingDayCalendar WithoutHolidays { get; } = new([]);
}
