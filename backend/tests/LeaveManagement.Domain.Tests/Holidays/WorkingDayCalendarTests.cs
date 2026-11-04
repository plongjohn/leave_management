using LeaveManagement.Domain.Common;
using LeaveManagement.Domain.Holidays;
using LeaveManagement.Domain.Tests.TestData;

namespace LeaveManagement.Domain.Tests.Holidays;

public sealed class WorkingDayCalendarTests
{
    [Fact]
    public void CountWorkingDays_WeekWithoutHolidays_ReturnsFive()
    {
        var week = new DateRange(new DateOnly(2026, 5, 4), new DateOnly(2026, 5, 10));

        Assert.Equal(5m, TestCalendars.Austria.CountWorkingDays(week));
    }

    [Fact]
    public void CountWorkingDays_WeekWithPublicHoliday_ExcludesHoliday()
    {
        // Ascension Day on Thursday, 14 May 2026.
        var week = new DateRange(new DateOnly(2026, 5, 11), new DateOnly(2026, 5, 15));

        Assert.Equal(4m, TestCalendars.Austria.CountWorkingDays(week));
    }

    [Fact]
    public void CountWorkingDays_WeekendOnly_ReturnsZero()
    {
        var weekend = new DateRange(new DateOnly(2026, 5, 16), new DateOnly(2026, 5, 17));

        Assert.Equal(0m, TestCalendars.Austria.CountWorkingDays(weekend));
    }

    [Fact]
    public void CountWorkingDays_ChristmasWithCompanyHalfDays_CountsHalfDays()
    {
        // 22, 23, 28, 29, 30 December are full days; 24 and 31 half days; 25 is Christmas Day.
        var christmas = new DateRange(new DateOnly(2026, 12, 22), new DateOnly(2026, 12, 31));

        Assert.Equal(6m, TestCalendars.Austria.CountWorkingDays(christmas));
    }

    [Fact]
    public void WorkingDaysOn_CompanyDayOff_ReturnsZero()
    {
        var bridgeDay = new DateOnly(2026, 5, 15);
        var calendar = new WorkingDayCalendar([Holiday.Create(bridgeDay, "Fenstertag", HolidayKind.CompanyDayOff)]);

        Assert.Equal(0m, calendar.WorkingDaysOn(bridgeDay));
    }

    [Fact]
    public void WorkingDaysOn_HolidayOnWeekend_ReturnsZero()
    {
        // Assumption Day 2026 falls on a Saturday.
        Assert.Equal(0m, TestCalendars.Austria.WorkingDaysOn(new DateOnly(2026, 8, 15)));
    }
}
