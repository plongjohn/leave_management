using System.Globalization;
using LeaveManagement.Domain.Common;

namespace LeaveManagement.Domain.Tests.Common;

public sealed class DateRangeTests
{
    [Fact]
    public void Constructor_EndBeforeStart_Throws()
    {
        Assert.Throws<ArgumentException>(() => new DateRange(new DateOnly(2026, 5, 4), new DateOnly(2026, 5, 3)));
    }

    [Fact]
    public void Constructor_SingleDay_IsValid()
    {
        var day = new DateOnly(2026, 5, 4);

        var range = new DateRange(day, day);

        Assert.Equal([day], range.Days());
    }

    [Theory]
    [InlineData("2026-05-01", "2026-05-03", true)]   // ends on the first day of the other range
    [InlineData("2026-05-08", "2026-05-12", true)]   // starts on the last day of the other range
    [InlineData("2026-05-05", "2026-05-06", true)]   // lies within the other range
    [InlineData("2026-04-01", "2026-05-31", true)]   // encloses the other range
    [InlineData("2026-05-01", "2026-05-02", false)]  // ends the day before
    [InlineData("2026-05-09", "2026-05-12", false)]  // starts the day after
    public void Overlaps_OtherRange_ReturnsExpected(string start, string end, bool expected)
    {
        var range = new DateRange(new DateOnly(2026, 5, 3), new DateOnly(2026, 5, 8));
        var other = new DateRange(DateOnly.Parse(start, CultureInfo.InvariantCulture), DateOnly.Parse(end, CultureInfo.InvariantCulture));

        Assert.Equal(expected, range.Overlaps(other));
        Assert.Equal(expected, other.Overlaps(range));
    }

    [Fact]
    public void Days_RangeOverYearEnd_ReturnsEveryDay()
    {
        var range = new DateRange(new DateOnly(2026, 12, 30), new DateOnly(2027, 1, 2));

        Assert.Equal(
            [new DateOnly(2026, 12, 30), new DateOnly(2026, 12, 31), new DateOnly(2027, 1, 1), new DateOnly(2027, 1, 2)],
            range.Days());
    }

    [Theory]
    [InlineData("2026-05-04", "2026-05-04", 1)]
    [InlineData("2026-06-12", "2026-06-15", 4)]   // Friday to Monday, weekend included
    [InlineData("2026-12-30", "2027-01-02", 4)]
    public void CalendarDays_Range_CountsBothEnds(string start, string end, int expected)
    {
        var range = new DateRange(DateOnly.Parse(start, CultureInfo.InvariantCulture), DateOnly.Parse(end, CultureInfo.InvariantCulture));

        Assert.Equal(expected, range.CalendarDays);
    }
}
