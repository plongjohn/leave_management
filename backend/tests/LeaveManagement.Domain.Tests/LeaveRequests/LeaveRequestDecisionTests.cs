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
            LeaveRequestErrors.NotResponsibleManager,
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

    [Fact(Skip = "Phase 5: HR decides on requests of employees without a manager (needs roles).")]
    public void Approve_RequesterWithoutManagerByHr_SetsApproved()
    {
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
            LeaveRequestErrors.NotResponsibleManager,
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
