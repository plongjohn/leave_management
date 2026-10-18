using System.Globalization;
using LeaveManagement.Domain.Employees;
using LeaveManagement.Domain.LeaveRequests;
using LeaveManagement.Domain.Tests.TestData;

namespace LeaveManagement.Domain.Tests.LeaveRequests;

public sealed class SpecialLeaveTests
{
    // Entry date 1 February 2020: working years run from 1 February to 31 January.
    private readonly Employee _employee = TestEmployees.ReportOf(TestEmployees.Manager());

    [Fact]
    public void Submit_SpecialLeaveWithinLimit_IsPendingWithReason()
    {
        var request = SubmitSpecialLeave(SpecialLeaveReason.Relocation, new DateOnly(2026, 6, 8), new DateOnly(2026, 6, 9));

        Assert.Equal(LeaveRequestStatus.Pending, request.Status);
        Assert.Equal(SpecialLeaveReason.Relocation, request.SpecialLeaveReason);
    }

    [Fact]
    public void Submit_SpecialLeaveExceedsMaximum_Throws()
    {
        AssertRule.Violated(
            LeaveRequestErrors.SpecialLeaveLimitExceeded,
            () => SubmitSpecialLeave(SpecialLeaveReason.OwnWedding, new DateOnly(2026, 6, 8), new DateOnly(2026, 6, 11)));
    }

    [Fact]
    public void Submit_SpecialLeave_DoesNotUseEntitlement()
    {
        var request = TestRequests.Submit(
            _employee,
            new DateOnly(2026, 6, 8),
            new DateOnly(2026, 6, 10),
            LeaveType.SpecialLeave,
            entitlements: [],
            specialLeaveReason: SpecialLeaveReason.DeathOfCloseRelative);

        Assert.Equal(3m, request.WorkingDays);
    }

    [Fact]
    public void Submit_SpecialLeaveWithoutReason_Throws()
    {
        AssertRule.Violated(
            LeaveRequestErrors.SpecialLeaveReasonRequired,
            () => TestRequests.Submit(_employee, new DateOnly(2026, 6, 8), new DateOnly(2026, 6, 8), LeaveType.SpecialLeave));
    }

    [Fact]
    public void Submit_ReasonForVacation_Throws()
    {
        AssertRule.Violated(
            LeaveRequestErrors.SpecialLeaveReasonNotAllowed,
            () => TestRequests.Submit(
                _employee,
                new DateOnly(2026, 6, 8),
                new DateOnly(2026, 6, 8),
                specialLeaveReason: SpecialLeaveReason.Relocation));
    }

    [Fact]
    public void Submit_CareLeaveWithinWorkingYear_IsAllowed()
    {
        var request = SubmitSpecialLeave(SpecialLeaveReason.CareLeave, new DateOnly(2026, 6, 8), new DateOnly(2026, 6, 12));

        Assert.Equal(5m, request.WorkingDays);
    }

    [Fact]
    public void Submit_CareLeaveExceedsWeekPerWorkingYear_Throws()
    {
        var earlier = SubmitSpecialLeave(SpecialLeaveReason.CareLeave, new DateOnly(2026, 3, 9), new DateOnly(2026, 3, 11));

        AssertRule.Violated(
            LeaveRequestErrors.SpecialLeaveLimitExceeded,
            () => SubmitSpecialLeave(
                SpecialLeaveReason.CareLeave, new DateOnly(2026, 6, 8), new DateOnly(2026, 6, 10), existingRequests: [earlier]));
    }

    [Fact]
    public void Submit_CareLeaveInNextWorkingYear_IsAllowed()
    {
        // 26–30 January 2026 still belongs to the working year that started on 1 February 2025.
        var lastWorkingYear = TestRequests.Submit(
            _employee,
            new DateOnly(2026, 1, 26),
            new DateOnly(2026, 1, 30),
            LeaveType.SpecialLeave,
            today: new DateOnly(2026, 1, 2),
            specialLeaveReason: SpecialLeaveReason.CareLeave);

        var request = SubmitSpecialLeave(
            SpecialLeaveReason.CareLeave, new DateOnly(2026, 3, 9), new DateOnly(2026, 3, 13), existingRequests: [lastWorkingYear]);

        Assert.Equal(5m, request.WorkingDays);
    }

    [Theory]
    [InlineData("2020-02-01", "2026-01-31", "2025-02-01")]
    [InlineData("2020-02-01", "2026-02-01", "2026-02-01")]
    [InlineData("2020-02-29", "2026-03-01", "2026-02-28")]   // entry on a leap day
    public void WorkingYearStart_Date_ReturnsLastAnniversary(string entryDate, string date, string expected)
    {
        Assert.Equal(
            DateOnly.Parse(expected, CultureInfo.InvariantCulture),
            SpecialLeavePolicy.WorkingYearStart(
                DateOnly.Parse(entryDate, CultureInfo.InvariantCulture),
                DateOnly.Parse(date, CultureInfo.InvariantCulture)));
    }

    [Fact]
    public void MaxWorkingDaysPerOccasion_CareLeave_ReturnsNull()
    {
        Assert.Null(SpecialLeavePolicy.MaxWorkingDaysPerOccasion(SpecialLeaveReason.CareLeave));
        Assert.Equal(3m, SpecialLeavePolicy.MaxWorkingDaysPerOccasion(SpecialLeaveReason.OwnWedding));
    }

    private LeaveRequest SubmitSpecialLeave(
        SpecialLeaveReason reason,
        DateOnly start,
        DateOnly end,
        IReadOnlyCollection<LeaveRequest>? existingRequests = null) =>
        TestRequests.Submit(
            _employee, start, end, LeaveType.SpecialLeave, existingRequests, specialLeaveReason: reason);
}
