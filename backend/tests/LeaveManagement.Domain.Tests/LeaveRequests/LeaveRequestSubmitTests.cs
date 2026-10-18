using LeaveManagement.Domain.Employees;
using LeaveManagement.Domain.LeaveRequests;
using LeaveManagement.Domain.Tests.TestData;

namespace LeaveManagement.Domain.Tests.LeaveRequests;

public sealed class LeaveRequestSubmitTests
{
    private readonly Employee _employee = TestEmployees.ReportOf(TestEmployees.Manager());

    [Fact]
    public void Submit_Vacation_IsPendingWithWorkingDaySnapshot()
    {
        // Ascension Day on Thursday, 14 May 2026, is not a working day.
        var request = TestRequests.Submit(_employee, new DateOnly(2026, 5, 11), new DateOnly(2026, 5, 15));

        Assert.Equal(LeaveRequestStatus.Pending, request.Status);
        Assert.Equal(_employee.Id, request.EmployeeId);
        Assert.Equal(4m, request.WorkingDays);
        Assert.Equal(
            [new DateOnly(2026, 5, 11), new DateOnly(2026, 5, 12), new DateOnly(2026, 5, 13), new DateOnly(2026, 5, 15)],
            request.Days.Select(day => day.Date));
        Assert.Equal(TestRequests.Now, request.CreatedAt);
        Assert.Null(request.DecidedById);
    }

    [Fact]
    public void Submit_NowWithOffset_StoresCreatedAtInUtc()
    {
        var request = TestRequests.Submit(_employee, new DateOnly(2026, 6, 8), new DateOnly(2026, 6, 8));

        Assert.Equal(TimeSpan.Zero, request.CreatedAt.Offset);
        Assert.Equal(TestRequests.Now, request.CreatedAt);
    }

    [Fact]
    public void Submit_OverYearEnd_KeepsDaysOfBothYears()
    {
        // 29 and 30 December full, 31 December half, 1 January holiday, 4 and 5 January full.
        var request = TestRequests.Submit(_employee, new DateOnly(2026, 12, 29), new DateOnly(2027, 1, 5));

        Assert.Equal(4.5m, request.WorkingDays);
        Assert.Equal(2.5m, request.Days.Where(day => day.Date.Year == 2026).Sum(day => day.WorkingDays));
        Assert.Equal(2m, request.Days.Where(day => day.Date.Year == 2027).Sum(day => day.WorkingDays));
    }

    [Fact]
    public void Submit_StartingToday_IsAllowed()
    {
        var request = TestRequests.Submit(_employee, TestRequests.Today, TestRequests.Today);

        Assert.Equal(1m, request.WorkingDays);
    }

    [Theory]
    [InlineData(LeaveType.Vacation)]
    [InlineData(LeaveType.TimeOffInLieu)]
    public void Submit_StartsInPast_Throws(LeaveType type)
    {
        var yesterday = TestRequests.Today.AddDays(-1);

        AssertRule.Violated(
            LeaveRequestErrors.StartsInPast,
            () => TestRequests.Submit(_employee, yesterday, TestRequests.Today.AddDays(2), type));
    }

    [Fact]
    public void Submit_SickLeaveInPast_IsApprovedImmediately()
    {
        var request = TestRequests.Submit(
            _employee, new DateOnly(2026, 2, 23), new DateOnly(2026, 2, 25), LeaveType.SickLeave);

        Assert.Equal(LeaveRequestStatus.Approved, request.Status);
        Assert.Equal(3m, request.WorkingDays);
    }

    [Fact]
    public void Submit_BeforeEntryDate_Throws()
    {
        var newcomer = TestEmployees.ReportOf(TestEmployees.Manager(), entryDate: new DateOnly(2026, 4, 1));

        AssertRule.Violated(
            LeaveRequestErrors.StartsBeforeEntryDate,
            () => TestRequests.Submit(newcomer, new DateOnly(2026, 3, 30), new DateOnly(2026, 4, 3)));
    }

    [Fact]
    public void Submit_OnlyNonWorkingDays_Throws()
    {
        AssertRule.Violated(
            LeaveRequestErrors.NoWorkingDays,
            () => TestRequests.Submit(_employee, new DateOnly(2026, 3, 7), new DateOnly(2026, 3, 8)));
    }

    [Fact]
    public void Submit_OverlappingActiveRequest_Throws()
    {
        var existing = TestRequests.Submit(_employee, new DateOnly(2026, 6, 8), new DateOnly(2026, 6, 12));

        AssertRule.Violated(
            LeaveRequestErrors.Overlapping,
            () => TestRequests.Submit(
                _employee, new DateOnly(2026, 6, 12), new DateOnly(2026, 6, 16), existingRequests: [existing]));
    }

    [Fact]
    public void Submit_OverlappingCancelledRequest_IsAllowed()
    {
        var cancelled = TestRequests.Submit(_employee, new DateOnly(2026, 6, 8), new DateOnly(2026, 6, 12));
        cancelled.Cancel(TestRequests.Today);

        var request = TestRequests.Submit(
            _employee, new DateOnly(2026, 6, 8), new DateOnly(2026, 6, 12), existingRequests: [cancelled]);

        Assert.Equal(LeaveRequestStatus.Pending, request.Status);
    }

    [Fact]
    public void Submit_OverlappingRejectedRequest_IsAllowed()
    {
        var manager = TestEmployees.Manager();
        var employee = TestEmployees.ReportOf(manager);
        var rejected = TestRequests.Submit(employee, new DateOnly(2026, 6, 8), new DateOnly(2026, 6, 12));
        rejected.Reject(manager, employee, TestRequests.Now);

        var request = TestRequests.Submit(
            employee, new DateOnly(2026, 6, 8), new DateOnly(2026, 6, 12), existingRequests: [rejected]);

        Assert.Equal(LeaveRequestStatus.Pending, request.Status);
    }

    [Fact]
    public void Submit_OverlappingRequestOfOtherEmployee_IsAllowed()
    {
        var colleagues = TestRequests.Submit(
            TestEmployees.Colleague(), new DateOnly(2026, 6, 8), new DateOnly(2026, 6, 12));

        var request = TestRequests.Submit(
            _employee, new DateOnly(2026, 6, 8), new DateOnly(2026, 6, 12), existingRequests: [colleagues]);

        Assert.Equal(LeaveRequestStatus.Pending, request.Status);
    }

    [Fact]
    public void Submit_Comment_IsTrimmed()
    {
        var request = TestRequests.Submit(
            _employee, new DateOnly(2026, 6, 8), new DateOnly(2026, 6, 8), comment: "  Arzttermin  ");

        Assert.Equal("Arzttermin", request.Comment);
    }

    [Fact]
    public void Submit_WhitespaceComment_IsStoredAsNull()
    {
        var request = TestRequests.Submit(
            _employee, new DateOnly(2026, 6, 8), new DateOnly(2026, 6, 8), comment: "   ");

        Assert.Null(request.Comment);
    }

    [Fact]
    public void Submit_CommentTooLong_Throws()
    {
        var comment = new string('x', LeaveRequest.CommentMaxLength + 1);

        Assert.Throws<ArgumentException>(
            () => TestRequests.Submit(_employee, new DateOnly(2026, 6, 8), new DateOnly(2026, 6, 8), comment: comment));
    }

    [Fact(Skip = "Phase 6: sick leave of more than three days during approved vacation returns the vacation days (§ 5 UrlG).")]
    public void Submit_SickLeaveDuringApprovedVacation_ReturnsVacationDays()
    {
    }
}
