using System.Net;
using System.Net.Http.Json;
using LeaveManagement.Api.Common;
using LeaveManagement.Api.LeaveRequests;
using LeaveManagement.Api.Tests.Infrastructure;
using LeaveManagement.Domain.Employees;
using LeaveManagement.Domain.LeaveRequests;
using LeaveManagement.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LeaveManagement.Api.Tests.LeaveRequests;

public sealed class CancelLeaveRequestTests : ApiTestBase
{
    private readonly Employee _manager;
    private readonly Employee _employee;

    public CancelLeaveRequestTests(ApiTestFixture fixture)
        : base(fixture)
    {
        _manager = Employee.Create("Markus", "Gruber", "markus.gruber@example.at", new DateOnly(2017, 9, 1), EmployeeRole.Manager);
        _employee = Employee.Create("Lena", "Wagner", "lena.wagner@example.at", new DateOnly(2020, 2, 1), EmployeeRole.Employee, _manager.Id);
    }

    [Fact]
    public async Task Cancel_Pending_SetsCancelled()
    {
        var request = TestLeaveRequests.Submit(_employee, new DateOnly(2026, 6, 8), new DateOnly(2026, 6, 12));
        await SignInWithManagerAsync(_manager, _employee, dbContext => dbContext.LeaveRequests.Add(request));

        var response = await CancelAsync(request.Id, request.Version);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(LeaveRequestStatus.Cancelled, (await LoadAsync(request.Id)).Status);
    }

    [Fact]
    public async Task Cancel_DecidedSinceLoaded_ReturnsConflictAndKeepsDecision()
    {
        var request = TestLeaveRequests.Submit(_employee, new DateOnly(2026, 6, 8), new DateOnly(2026, 6, 12));
        await SignInWithManagerAsync(_manager, _employee, dbContext => dbContext.LeaveRequests.Add(request));
        var versionSeenByEmployee = request.Version;

        // The manager approves while the employee still sees the pending request.
        await using (var scope = CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<LeaveManagementDbContext>();
            var stored = await dbContext.LeaveRequests.SingleAsync(entry => entry.Id == request.Id, CancellationToken);
            stored.Approve(_manager, _employee, TestLeaveRequests.Now);
            await dbContext.SaveChangesAsync(CancellationToken);
        }

        var response = await CancelAsync(request.Id, versionSeenByEmployee);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(CancellationToken);
        Assert.Equal(ApiErrors.ConcurrencyConflict, problem!.Extensions["code"]?.ToString());
        Assert.Equal(LeaveRequestStatus.Approved, (await LoadAsync(request.Id)).Status);
    }

    [Fact]
    public async Task Cancel_ApprovedAndStarted_ReturnsUnprocessableEntityWithCode()
    {
        var request = TestLeaveRequests.Approved(_manager, _employee, new DateOnly(2026, 3, 9), new DateOnly(2026, 3, 13));
        await SignInWithManagerAsync(_manager, _employee, dbContext => dbContext.LeaveRequests.Add(request));
        Factory.CompanyTime.UtcNow += TimeSpan.FromDays(7);

        var response = await CancelAsync(request.Id, request.Version);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(CancellationToken);
        Assert.Equal(LeaveRequestErrors.AlreadyStarted, problem!.Extensions["code"]?.ToString());
    }

    [Fact]
    public async Task Cancel_RequestOfSomeoneElse_ReturnsNotFoundAndKeepsIt()
    {
        var colleague = Employee.Create("Thomas", "Bauer", "thomas.bauer@example.at", new DateOnly(2021, 6, 15), EmployeeRole.Employee, _manager.Id);
        var foreign = TestLeaveRequests.Submit(colleague, new DateOnly(2026, 6, 8), new DateOnly(2026, 6, 12));
        await SignInWithManagerAsync(_manager, _employee, dbContext =>
        {
            dbContext.Employees.Add(colleague);
            dbContext.LeaveRequests.Add(foreign);
        });

        var response = await CancelAsync(foreign.Id, foreign.Version);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(LeaveRequestStatus.Pending, (await LoadAsync(foreign.Id)).Status);
    }

    [Fact]
    public async Task Cancel_WithoutToken_ReturnsUnauthorized()
    {
        var response = await CancelAsync(Guid.NewGuid(), 1);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private Task<HttpResponseMessage> CancelAsync(Guid id, uint version) =>
        Client.PostAsJsonAsync($"/api/me/leave-requests/{id}/cancel", new CancelLeaveRequestRequest(version), CancellationToken);

    private async Task<LeaveRequest> LoadAsync(Guid id)
    {
        await using var scope = CreateScope();
        return await scope.ServiceProvider.GetRequiredService<LeaveManagementDbContext>()
            .LeaveRequests.AsNoTracking().SingleAsync(request => request.Id == id, CancellationToken);
    }
}
