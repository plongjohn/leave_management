using LeaveManagement.Domain.Employees;
using LeaveManagement.Domain.LeaveRequests;
using LeaveManagement.Domain.Tests.TestData;

namespace LeaveManagement.Domain.Tests.LeaveRequests;

public sealed class LeaveRequestDecisionTests
{
    private readonly Employee _manager;
    private readonly Employee _employee;
    private readonly LeaveRequest _request;

    public LeaveRequestDecisionTests()
    {
        _manager = TestEmployees.Manager();
        _employee = TestEmployees.ReportOf(_manager);
        _request = TestRequests.Submit(_employee, new DateOnly(2026, 6, 8), new DateOnly(2026, 6, 12));
    }

    [Fact]
    public void Approve_ByOwnManager_SetsApprovedAndDecision()
    {
        var decidedAt = TestRequests.Now.AddHours(2);

        _request.Approve(_manager, _employee, decidedAt);

        Assert.Equal(LeaveRequestStatus.Approved, _request.Status);
        Assert.Equal(_manager.Id, _request.DecidedById);
        Assert.Equal(decidedAt, _request.DecidedAt);
    }

    [Fact]
    public void Approve_NowWithOffset_StoresDecidedAtInUtc()
    {
        _request.Approve(_manager, _employee, TestRequests.Now);

        Assert.Equal(TimeSpan.Zero, _request.DecidedAt?.Offset);
        Assert.Equal(TestRequests.Now, _request.DecidedAt);
    }

    [Fact]
    public void Approve_ByOtherEmployee_Throws()
    {
        AssertRule.Violated(
            LeaveRequestErrors.NotResponsibleApprover,
            () => _request.Approve(TestEmployees.Colleague(), _employee, TestRequests.Now));
    }

    [Fact]
    public void Approve_OwnRequest_Throws()
    {
        var managersRequest = TestRequests.Submit(_manager, new DateOnly(2026, 6, 8), new DateOnly(2026, 6, 12));

        AssertRule.Violated(
            LeaveRequestErrors.OwnRequest,
            () => managersRequest.Approve(_manager, _manager, TestRequests.Now));
    }

    [Fact]
    public void Approve_AlreadyRejected_Throws()
    {
        _request.Reject(_manager, _employee, TestRequests.Now);

        AssertRule.Violated(
            LeaveRequestErrors.NotPending,
            () => _request.Approve(_manager, _employee, TestRequests.Now));
    }

    [Fact]
    public void Approve_RequesterIsNotTheEmployee_Throws()
    {
        Assert.Throws<ArgumentException>(
            () => _request.Approve(_manager, TestEmployees.Colleague(), TestRequests.Now));
    }

    [Fact]
    public void Approve_RequesterWithoutManagerByHr_SetsApproved()
    {
        var hr = TestEmployees.Hr();
        var director = TestEmployees.WithoutManager();
        var request = TestRequests.Submit(director, new DateOnly(2026, 6, 8), new DateOnly(2026, 6, 12));

        request.Approve(hr, director, TestRequests.Now);

        Assert.Equal(LeaveRequestStatus.Approved, request.Status);
        Assert.Equal(hr.Id, request.DecidedById);
    }

    [Fact]
    public void Approve_RequesterWithoutManagerByNonHr_Throws()
    {
        var director = TestEmployees.WithoutManager();
        var request = TestRequests.Submit(director, new DateOnly(2026, 6, 8), new DateOnly(2026, 6, 12));

        AssertRule.Violated(
            LeaveRequestErrors.NotResponsibleApprover,
            () => request.Approve(_manager, director, TestRequests.Now));
    }

    [Fact]
    public void Approve_RequesterWithManagerByHr_Throws()
    {
        AssertRule.Violated(
            LeaveRequestErrors.NotResponsibleApprover,
            () => _request.Approve(TestEmployees.Hr(), _employee, TestRequests.Now));
    }

    [Fact]
    public void Approve_OwnRequestByHrWithoutManager_Throws()
    {
        var hr = TestEmployees.WithoutManager(EmployeeRole.Hr);
        var request = TestRequests.Submit(hr, new DateOnly(2026, 6, 8), new DateOnly(2026, 6, 12));

        AssertRule.Violated(LeaveRequestErrors.OwnRequest, () => request.Approve(hr, hr, TestRequests.Now));
    }

    [Fact]
    public void Reject_RequesterWithoutManagerByHr_SetsRejected()
    {
        var hr = TestEmployees.Hr();
        var director = TestEmployees.WithoutManager();
        var request = TestRequests.Submit(director, new DateOnly(2026, 6, 8), new DateOnly(2026, 6, 12));

        request.Reject(hr, director, TestRequests.Now);

        Assert.Equal(LeaveRequestStatus.Rejected, request.Status);
    }

    [Fact]
    public void Reject_ByOwnManager_SetsRejectedAndDecision()
    {
        _request.Reject(_manager, _employee, TestRequests.Now);

        Assert.Equal(LeaveRequestStatus.Rejected, _request.Status);
        Assert.Equal(_manager.Id, _request.DecidedById);
        Assert.Equal(TestRequests.Now, _request.DecidedAt);
    }

    [Fact]
    public void Reject_ByOtherEmployee_Throws()
    {
        AssertRule.Violated(
            LeaveRequestErrors.NotResponsibleApprover,
            () => _request.Reject(TestEmployees.Colleague(), _employee, TestRequests.Now));
    }

    [Fact]
    public void Reject_AlreadyApproved_Throws()
    {
        _request.Approve(_manager, _employee, TestRequests.Now);

        AssertRule.Violated(
            LeaveRequestErrors.NotPending,
            () => _request.Reject(_manager, _employee, TestRequests.Now));
    }

    [Fact]
    public void Cancel_Pending_SetsCancelled()
    {
        _request.Cancel(TestRequests.Today);

        Assert.Equal(LeaveRequestStatus.Cancelled, _request.Status);
    }

    [Fact]
    public void Cancel_PendingAfterStart_SetsCancelled()
    {
        _request.Cancel(_request.StartDate.AddDays(1));

        Assert.Equal(LeaveRequestStatus.Cancelled, _request.Status);
    }

    [Fact]
    public void Cancel_ApprovedBeforeStart_SetsCancelled()
    {
        _request.Approve(_manager, _employee, TestRequests.Now);

        _request.Cancel(_request.StartDate.AddDays(-1));

        Assert.Equal(LeaveRequestStatus.Cancelled, _request.Status);
    }

    [Fact]
    public void Cancel_ApprovedOnStartDay_Throws()
    {
        _request.Approve(_manager, _employee, TestRequests.Now);

        AssertRule.Violated(LeaveRequestErrors.AlreadyStarted, () => _request.Cancel(_request.StartDate));
    }

    [Fact]
    public void Cancel_Rejected_Throws()
    {
        _request.Reject(_manager, _employee, TestRequests.Now);

        AssertRule.Violated(LeaveRequestErrors.NotCancellable, () => _request.Cancel(TestRequests.Today));
    }

    [Fact]
    public void Cancel_AlreadyCancelled_Throws()
    {
        _request.Cancel(TestRequests.Today);

        AssertRule.Violated(LeaveRequestErrors.NotCancellable, () => _request.Cancel(TestRequests.Today));
    }
}
