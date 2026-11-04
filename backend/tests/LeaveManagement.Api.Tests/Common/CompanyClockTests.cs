using System.Globalization;
using LeaveManagement.Api.Common;
using Microsoft.Extensions.Time.Testing;

namespace LeaveManagement.Api.Tests.Common;

public sealed class CompanyClockTests
{
    [Theory]
    // Winter time (UTC+1): New Year's Eve 23:30 UTC is already 1 January in Vienna.
    [InlineData("2026-12-31T22:59:00Z", "2026-12-31")]
    [InlineData("2026-12-31T23:30:00Z", "2027-01-01")]
    // Summer time (UTC+2).
    [InlineData("2026-07-31T21:59:00Z", "2026-07-31")]
    [InlineData("2026-07-31T22:30:00Z", "2026-08-01")]
    public void Today_UtcTime_ReturnsDateInVienna(string utcNow, string expectedToday)
    {
        var clock = new CompanyClock(new FakeTimeProvider(DateTimeOffset.Parse(utcNow, CultureInfo.InvariantCulture)));

        Assert.Equal(DateOnly.Parse(expectedToday, CultureInfo.InvariantCulture), clock.Today());
    }

    [Fact]
    public void Now_Always_ReturnsUtc()
    {
        var utcNow = new DateTimeOffset(2026, 11, 3, 9, 15, 0, TimeSpan.Zero);
        var clock = new CompanyClock(new FakeTimeProvider(utcNow));

        Assert.Equal(utcNow, clock.Now());
        Assert.Equal(TimeSpan.Zero, clock.Now().Offset);
    }
}
