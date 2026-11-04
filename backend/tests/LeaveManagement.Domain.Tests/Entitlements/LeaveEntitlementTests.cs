using LeaveManagement.Domain.Employees;
using LeaveManagement.Domain.Entitlements;
using LeaveManagement.Domain.Tests.TestData;

namespace LeaveManagement.Domain.Tests.Entitlements;

public sealed class LeaveEntitlementTests
{
    private readonly Employee _manager;
    private readonly Employee _employee;

    public LeaveEntitlementTests()
    {
        _manager = TestEmployees.Manager();
        _employee = TestEmployees.ReportOf(_manager);
    }

    [Fact]
    public void Create_WithoutDays_UsesStandardDays()
    {
        var entitlement = LeaveEntitlement.Create(_employee.Id, 2026);

        Assert.Equal(25m, entitlement.Days);
        Assert.Equal(new DateOnly(2028, 12, 31), entitlement.ExpiresOn);
    }

    [Fact]
    public void Create_NegativeDays_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => LeaveEntitlement.Create(_employee.Id, 2026, -1m));
    }

    [Fact]
    public void Create_DaysNotInHalfDaySteps_Throws()
    {
        Assert.Throws<ArgumentException>(() => LeaveEntitlement.Create(_employee.Id, 2026, 25.3m));
    }

    [Fact]
    public void ChangeDays_Increase_SetsDays()
    {
        var entitlement = LeaveEntitlement.Create(_employee.Id, 2026);

        entitlement.ChangeDays(30m, [entitlement], []);

        Assert.Equal(30m, entitlement.Days);
    }

    [Fact]
    public void ChangeDays_DecreaseAboveTakenDays_SetsDays()
    {
        var entitlement = LeaveEntitlement.Create(_employee.Id, 2026);
        var request = TestRequests.Approved(_manager, _employee, new DateOnly(2026, 6, 8), new DateOnly(2026, 6, 12));

        entitlement.ChangeDays(5m, [entitlement], [request]);

        Assert.Equal(5m, entitlement.Days);
    }

    [Fact]
    public void ChangeDays_BelowTakenDays_Throws()
    {
        var entitlement = LeaveEntitlement.Create(_employee.Id, 2026);
        var approved = TestRequests.Approved(_manager, _employee, new DateOnly(2026, 6, 8), new DateOnly(2026, 6, 12));
        var pending = TestRequests.Submit(_employee, new DateOnly(2026, 8, 3), new DateOnly(2026, 8, 4));

        AssertRule.Violated(
            EntitlementErrors.TakenDaysNotCovered,
            () => entitlement.ChangeDays(6m, [entitlement], [approved, pending]));
        Assert.Equal(25m, entitlement.Days);
    }

    [Fact]
    public void ChangeDays_BelowTakenDaysButCoveredByPreviousYear_SetsDays()
    {
        var previousYear = LeaveEntitlement.Create(_employee.Id, 2025);
        var entitlement = LeaveEntitlement.Create(_employee.Id, 2026);
        var request = TestRequests.Approved(_manager, _employee, new DateOnly(2026, 6, 8), new DateOnly(2026, 6, 12));

        entitlement.ChangeDays(0m, [previousYear, entitlement], [request]);

        Assert.Equal(0m, entitlement.Days);
    }

    [Fact]
    public void ChangeDays_Negative_Throws()
    {
        var entitlement = LeaveEntitlement.Create(_employee.Id, 2026);

        Assert.Throws<ArgumentOutOfRangeException>(() => entitlement.ChangeDays(-0.5m, [entitlement], []));
    }
}
