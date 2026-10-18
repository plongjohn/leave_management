using LeaveManagement.Domain.Employees;
using LeaveManagement.Domain.Entitlements;
using LeaveManagement.Domain.LeaveRequests;
using LeaveManagement.Domain.Tests.TestData;

namespace LeaveManagement.Domain.Tests.Entitlements;

public sealed class LeaveBalanceCalculatorTests
{
    private readonly Employee _manager;
    private readonly Employee _employee;

    public LeaveBalanceCalculatorTests()
    {
        _manager = TestEmployees.Manager();
        _employee = TestEmployees.ReportOf(_manager);
    }

    [Fact]
    public void Calculate_NoRequests_RemainingEqualsEntitlement()
    {
        var balance = LeaveBalanceCalculator.Calculate([Entitlement(2026, 25m)], []);

        var year = Assert.Single(balance.Years);
        Assert.Equal(25m, year.RemainingDays);
        Assert.Equal(new DateOnly(2028, 12, 31), year.ExpiresOn);
        Assert.True(balance.IsCovered);
    }

    [Fact]
    public void Calculate_ApprovedAndPendingRequests_AreCountedSeparately()
    {
        var approved = TestRequests.Approved(_manager, _employee, new DateOnly(2026, 6, 8), new DateOnly(2026, 6, 12));
        var pending = TestRequests.Submit(_employee, new DateOnly(2026, 8, 3), new DateOnly(2026, 8, 4));

        var balance = LeaveBalanceCalculator.Calculate([Entitlement(2026, 25m)], [approved, pending]);

        var year = Assert.Single(balance.Years);
        Assert.Equal(5m, year.ApprovedDays);
        Assert.Equal(2m, year.PendingDays);
        Assert.Equal(18m, year.RemainingDays);
    }

    [Fact]
    public void Calculate_CancelledOrRejectedRequest_ReleasesDays()
    {
        var cancelled = TestRequests.Submit(_employee, new DateOnly(2026, 6, 8), new DateOnly(2026, 6, 12));
        cancelled.Cancel(TestRequests.Today);
        var rejected = TestRequests.Submit(_employee, new DateOnly(2026, 8, 3), new DateOnly(2026, 8, 4));
        rejected.Reject(_manager, _employee, TestRequests.Now);

        var balance = LeaveBalanceCalculator.Calculate([Entitlement(2026, 25m)], [cancelled, rejected]);

        Assert.Equal(25m, balance.AvailableIn(2026));
    }

    [Fact]
    public void Calculate_OtherLeaveTypes_DoNotUseEntitlement()
    {
        var timeOffInLieu = TestRequests.Submit(
            _employee, new DateOnly(2026, 6, 8), new DateOnly(2026, 6, 12), LeaveType.TimeOffInLieu);
        var sickLeave = TestRequests.Submit(
            _employee, new DateOnly(2026, 2, 23), new DateOnly(2026, 2, 25), LeaveType.SickLeave);

        var balance = LeaveBalanceCalculator.Calculate([Entitlement(2026, 25m)], [timeOffInLieu, sickLeave]);

        Assert.Equal(25m, balance.AvailableIn(2026));
    }

    [Fact]
    public void Calculate_RemainingDaysOfPreviousYear_AreUsedFirst()
    {
        var request = TestRequests.Approved(_manager, _employee, new DateOnly(2026, 6, 8), new DateOnly(2026, 6, 12));

        var balance = LeaveBalanceCalculator.Calculate([Entitlement(2025, 3m), Entitlement(2026, 25m)], [request]);

        Assert.Equal(3m, YearOf(balance, 2025).ApprovedDays);
        Assert.Equal(2m, YearOf(balance, 2026).ApprovedDays);
        Assert.Equal(23m, balance.AvailableIn(2026));
    }

    [Fact]
    public void Calculate_LastDayBeforeExpiry_UsesExpiringEntitlement()
    {
        // The entitlement of 2024 can be used until 31 December 2026.
        var request = TestRequests.Approved(_manager, _employee, new DateOnly(2026, 12, 30), new DateOnly(2026, 12, 30));

        var balance = LeaveBalanceCalculator.Calculate([Entitlement(2024, 5m), Entitlement(2026, 25m)], [request]);

        Assert.Equal(1m, YearOf(balance, 2024).ApprovedDays);
        Assert.Equal(0m, YearOf(balance, 2026).ApprovedDays);
    }

    [Fact]
    public void Calculate_ExpiredEntitlement_IsNotUsed()
    {
        var request = TestRequests.Approved(_manager, _employee, new DateOnly(2027, 1, 11), new DateOnly(2027, 1, 15));

        var balance = LeaveBalanceCalculator.Calculate([Entitlement(2024, 10m), Entitlement(2027, 2m)], [request]);

        Assert.Equal(0m, YearOf(balance, 2024).ApprovedDays);
        Assert.Equal(2m, YearOf(balance, 2027).ApprovedDays);
        Assert.Equal(3m, balance.UncoveredDays);
        Assert.False(balance.IsCovered);
        Assert.Equal(0m, balance.AvailableIn(2027));
    }

    [Fact]
    public void Calculate_EntitlementOfFutureYear_IsNotUsedInAdvance()
    {
        var request = TestRequests.Approved(_manager, _employee, new DateOnly(2026, 6, 8), new DateOnly(2026, 6, 12));

        var balance = LeaveBalanceCalculator.Calculate([Entitlement(2027, 25m)], [request]);

        Assert.Equal(5m, balance.UncoveredDays);
    }

    [Fact]
    public void Calculate_RequestOverYearEnd_UsesOldestUsableEntitlementPerDay()
    {
        // 2.5 days in 2026 and 2 days in 2027; the remaining 2026 entitlement covers both years.
        var request = TestRequests.Approved(_manager, _employee, new DateOnly(2026, 12, 29), new DateOnly(2027, 1, 5));

        var balance = LeaveBalanceCalculator.Calculate([Entitlement(2026, 25m), Entitlement(2027, 25m)], [request]);

        Assert.Equal(4.5m, YearOf(balance, 2026).ApprovedDays);
        Assert.Equal(0m, YearOf(balance, 2027).ApprovedDays);
    }

    [Fact]
    public void Calculate_RequestOverYearEnd_CannotUseNextYearForDaysOfThisYear()
    {
        var request = TestRequests.Approved(_manager, _employee, new DateOnly(2026, 12, 29), new DateOnly(2027, 1, 5));

        var balance = LeaveBalanceCalculator.Calculate([Entitlement(2026, 2m), Entitlement(2027, 25m)], [request]);

        Assert.Equal(2m, YearOf(balance, 2026).ApprovedDays);
        Assert.Equal(2m, YearOf(balance, 2027).ApprovedDays);
        Assert.Equal(0.5m, balance.UncoveredDays);
    }

    [Fact]
    public void Calculate_AllocatesInOrderOfLeaveDates()
    {
        // The January request was planned first, but the August request is taken first and uses
        // the 2026 entitlement, so January no longer fits.
        var january = TestRequests.Approved(_manager, _employee, new DateOnly(2027, 1, 11), new DateOnly(2027, 1, 15));
        var august = TestRequests.Approved(_manager, _employee, new DateOnly(2026, 8, 3), new DateOnly(2026, 8, 10));

        var balance = LeaveBalanceCalculator.Calculate([Entitlement(2026, 10m)], [january, august]);

        Assert.Equal(1m, balance.UncoveredDays);
    }

    [Fact]
    public void Calculate_TwoEntitlementsForSameYear_Throws()
    {
        Assert.Throws<ArgumentException>(
            () => LeaveBalanceCalculator.Calculate([Entitlement(2026, 25m), Entitlement(2026, 5m)], []));
    }

    private LeaveEntitlement Entitlement(int year, decimal days) => LeaveEntitlement.Create(_employee.Id, year, days);

    private static EntitlementYearBalance YearOf(LeaveBalance balance, int year) =>
        Assert.Single(balance.Years, entry => entry.Year == year);
}
