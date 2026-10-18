using System.Globalization;
using LeaveManagement.Domain.Holidays;

namespace LeaveManagement.Domain.Tests.Holidays;

public sealed class AustrianPublicHolidaysTests
{
    [Theory]
    [InlineData(2008, "2008-03-23")]   // early Easter
    [InlineData(2024, "2024-03-31")]
    [InlineData(2025, "2025-04-20")]
    [InlineData(2026, "2026-04-05")]
    [InlineData(2027, "2027-03-28")]
    [InlineData(2038, "2038-04-25")]   // latest possible date
    [InlineData(2285, "2285-03-22")]   // earliest possible date
    public void EasterSunday_Year_ReturnsKnownDate(int year, string expected)
    {
        Assert.Equal(Parse(expected), AustrianPublicHolidays.EasterSunday(year));
    }

    [Theory]
    [InlineData(2025, "2025-04-21", "2025-05-29", "2025-06-09", "2025-06-19")]
    [InlineData(2026, "2026-04-06", "2026-05-14", "2026-05-25", "2026-06-04")]
    [InlineData(2027, "2027-03-29", "2027-05-06", "2027-05-17", "2027-05-27")]
    public void For_Year_ContainsMovableHolidays(
        int year, string easterMonday, string ascensionDay, string whitMonday, string corpusChristi)
    {
        var holidays = AustrianPublicHolidays.For(year).ToDictionary(holiday => holiday.Name, holiday => holiday.Date);

        Assert.Equal(Parse(easterMonday), holidays["Ostermontag"]);
        Assert.Equal(Parse(ascensionDay), holidays["Christi Himmelfahrt"]);
        Assert.Equal(Parse(whitMonday), holidays["Pfingstmontag"]);
        Assert.Equal(Parse(corpusChristi), holidays["Fronleichnam"]);
    }

    [Fact]
    public void For_Year_ReturnsThirteenStatutoryHolidays()
    {
        var holidays = AustrianPublicHolidays.For(2026);

        Assert.Equal(13, holidays.Count);
        Assert.All(holidays, holiday => Assert.Equal(HolidayKind.Statutory, holiday.Kind));
        Assert.Equal(13, holidays.Select(holiday => holiday.Date).Distinct().Count());
    }

    [Fact]
    public void For_Year_DoesNotContainGoodFriday()
    {
        var goodFriday = AustrianPublicHolidays.EasterSunday(2026).AddDays(-2);

        Assert.DoesNotContain(AustrianPublicHolidays.For(2026), holiday => holiday.Date == goodFriday);
    }

    private static DateOnly Parse(string date) => DateOnly.Parse(date, CultureInfo.InvariantCulture);
}
