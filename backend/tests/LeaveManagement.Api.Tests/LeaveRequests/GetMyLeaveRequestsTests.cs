using System.Net;
using System.Net.Http.Json;
using LeaveManagement.Api.LeaveRequests;
using LeaveManagement.Api.Tests.Infrastructure;
using LeaveManagement.Domain.Employees;
using LeaveManagement.Domain.LeaveRequests;

namespace LeaveManagement.Api.Tests.LeaveRequests;

public sealed class GetMyLeaveRequestsTests : ApiTestBase
{
    private readonly Employee _manager;
    private readonly Employee _employee;

    public GetMyLeaveRequestsTests(ApiTestFixture fixture)
        : base(fixture)
    {
        _manager = Employee.Create("Markus", "Gruber", "markus.gruber@example.at", new DateOnly(2017, 9, 1), EmployeeRole.Manager);
        _employee = Employee.Create("Lena", "Wagner", "lena.wagner@example.at", new DateOnly(2020, 2, 1), EmployeeRole.Employee, _manager.Id);
    }

    [Fact]
    public async Task GetMyLeaveRequests_Year_ReturnsOwnRequestsOfThatYearLatestFirst()
    {
        var colleague = Employee.Create("Thomas", "Bauer", "thomas.bauer@example.at", new DateOnly(2021, 6, 15), EmployeeRole.Employee, _manager.Id);
        var june = TestLeaveRequests.Submit(_employee, new DateOnly(2026, 6, 8), new DateOnly(2026, 6, 12));
        var april = TestLeaveRequests.Submit(_employee, new DateOnly(2026, 4, 13), new DateOnly(2026, 4, 14), LeaveType.TimeOffInLieu);
        await SignInWithRequestsAsync(
            june,
            april,
            TestLeaveRequests.Submit(_employee, new DateOnly(2027, 1, 11), new DateOnly(2027, 1, 12)));
        await ArrangeAsync(dbContext =>
        {
            dbContext.Employees.Add(colleague);
            dbContext.LeaveRequests.Add(TestLeaveRequests.Submit(colleague, new DateOnly(2026, 6, 8), new DateOnly(2026, 6, 9)));
        });

        var requests = await GetAsync(2026);

        Assert.Equal([june.Id, april.Id], requests.Select(request => request.Id));
        Assert.Equal(5m, requests[0].WorkingDays);
        Assert.Equal(LeaveType.TimeOffInLieu, requests[1].Type);
    }

    [Fact]
    public async Task GetMyLeaveRequests_Pending_NamesTheManagerWhoDecides()
    {
        await SignInWithRequestsAsync(TestLeaveRequests.Submit(_employee, new DateOnly(2026, 6, 8), new DateOnly(2026, 6, 12)));

        var request = Assert.Single(await GetAsync(2026));

        Assert.Equal(LeaveRequestStatus.Pending, request.Status);
        Assert.Equal("Markus Gruber", request.ApproverName);
        Assert.Null(request.DecidedByName);
        Assert.True(request.CanBeCancelled);
        Assert.NotEqual(0u, request.Version);
    }

    [Fact]
    public async Task GetMyLeaveRequests_PendingWithoutManager_LeavesApproverEmptyForHr()
    {
        var director = Employee.Create("Andrea", "Steiner", "andrea.steiner@example.at", new DateOnly(2012, 1, 2), EmployeeRole.Manager);
        await SignInAsync(director);
        await ArrangeAsync(dbContext => dbContext.LeaveRequests.Add(
            TestLeaveRequests.Submit(director, new DateOnly(2026, 6, 8), new DateOnly(2026, 6, 12))));

        var request = Assert.Single(await GetAsync(2026));

        Assert.Null(request.ApproverName);
    }

    [Fact]
    public async Task GetMyLeaveRequests_ApprovedAndStarted_NamesDeciderAndCannotBeCancelled()
    {
        await SignInWithRequestsAsync(
            TestLeaveRequests.Approved(_manager, _employee, new DateOnly(2026, 3, 9), new DateOnly(2026, 3, 13)));
        Factory.CompanyTime.UtcNow += TimeSpan.FromDays(8);

        var request = Assert.Single(await GetAsync(2026));

        Assert.Equal("Markus Gruber", request.DecidedByName);
        Assert.Equal(TestLeaveRequests.Now, request.DecidedAt);
        Assert.Null(request.ApproverName);
        Assert.False(request.CanBeCancelled);
    }

    [Fact]
    public async Task GetMyLeaveRequests_VacationWithSickLeave_ReturnsReturnedDays()
    {
        var vacation = TestLeaveRequests.Approved(_manager, _employee, new DateOnly(2026, 6, 8), new DateOnly(2026, 6, 19));
        var sickLeave = TestLeaveRequests.Submit(
            _employee,
            new DateOnly(2026, 6, 10),
            new DateOnly(2026, 6, 12),
            LeaveType.SickLeave,
            existingRequests: [vacation]);
        await SignInWithRequestsAsync(vacation, sickLeave);

        var requests = await GetAsync(2026);

        Assert.Equal(3m, requests.Single(request => request.Id == vacation.Id).ReturnedDays);
        Assert.Equal(0m, requests.Single(request => request.Id == sickLeave.Id).ReturnedDays);
    }

    [Fact]
    public async Task GetMyLeaveRequests_YearOutOfRange_ReturnsBadRequest()
    {
        await SignInWithRequestsAsync();

        var response = await Client.GetAsync("/api/me/leave-requests?year=0", CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetMyLeaveRequests_WithoutToken_ReturnsUnauthorized()
    {
        var response = await Client.GetAsync("/api/me/leave-requests?year=2026", CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // The manager first, because the employee references them; the requests last.
    private async Task SignInWithRequestsAsync(params LeaveRequest[] requests)
    {
        await ArrangeAsync(dbContext => dbContext.Employees.Add(_manager));
        await SignInAsync(_employee);
        await ArrangeAsync(dbContext => dbContext.LeaveRequests.AddRange(requests));
    }

    private async Task<List<MyLeaveRequestResponse>> GetAsync(int year)
    {
        var response = await Client.GetAsync($"/api/me/leave-requests?year={year}", CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<List<MyLeaveRequestResponse>>(ApiJson.Options, CancellationToken))!;
    }
}
