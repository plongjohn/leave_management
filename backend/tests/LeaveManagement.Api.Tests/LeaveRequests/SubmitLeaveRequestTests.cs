using System.Net;
using System.Net.Http.Json;
using LeaveManagement.Api.LeaveRequests;
using LeaveManagement.Api.Tests.Infrastructure;
using LeaveManagement.Domain.Employees;
using LeaveManagement.Domain.Entitlements;
using LeaveManagement.Domain.LeaveRequests;
using LeaveManagement.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LeaveManagement.Api.Tests.LeaveRequests;

public sealed class SubmitLeaveRequestTests : ApiTestBase
{
    private const string Url = "/api/me/leave-requests";

    private readonly Employee _manager;
    private readonly Employee _employee;
    private readonly LeaveEntitlement _entitlement;

    public SubmitLeaveRequestTests(ApiTestFixture fixture)
        : base(fixture)
    {
        _manager = Employee.Create("Markus", "Gruber", "markus.gruber@example.at", new DateOnly(2017, 9, 1), EmployeeRole.Manager);
        _employee = Employee.Create("Lena", "Wagner", "lena.wagner@example.at", new DateOnly(2020, 2, 1), EmployeeRole.Employee, _manager.Id);
        _entitlement = LeaveEntitlement.Create(_employee.Id, 2026);
    }

    [Fact]
    public async Task Submit_Vacation_CreatesPendingRequestWithWorkingDaySnapshot()
    {
        await SignInWithManagerAsync(_manager, _employee, dbContext => dbContext.LeaveEntitlements.Add(_entitlement));

        var response = await PostAsync(new LeaveRequestDraftRequest(
            new DateOnly(2026, 5, 11), new DateOnly(2026, 5, 15), LeaveType.Vacation, "  Familienurlaub "));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<MyLeaveRequestResponse>(ApiJson.Options, CancellationToken);
        Assert.NotNull(created);
        Assert.Equal($"/api/me/leave-requests/{created.Id}", response.Headers.Location?.OriginalString);
        Assert.Equal(LeaveRequestStatus.Pending, created.Status);
        Assert.Equal("Familienurlaub", created.Comment);
        Assert.Equal("Markus Gruber", created.ApproverName);

        // Ascension Day (14 May 2026) is taken from the holidays in the database.
        await using var scope = CreateScope();
        var stored = await scope.ServiceProvider.GetRequiredService<LeaveManagementDbContext>()
            .LeaveRequests.SingleAsync(request => request.Id == created.Id, CancellationToken);
        Assert.Equal(4m, stored.WorkingDays);
        Assert.DoesNotContain(stored.Days, day => day.Date == new DateOnly(2026, 5, 14));
    }

    [Fact]
    public async Task Submit_Created_CanBeReadByLocation()
    {
        await SignInWithManagerAsync(_manager, _employee, dbContext => dbContext.LeaveEntitlements.Add(_entitlement));
        var response = await PostAsync(new LeaveRequestDraftRequest(new DateOnly(2026, 6, 8), new DateOnly(2026, 6, 8), LeaveType.TimeOffInLieu));

        var read = await Client.GetAsync(response.Headers.Location, CancellationToken);

        Assert.Equal(HttpStatusCode.OK, read.StatusCode);
    }

    [Fact]
    public async Task Submit_OverlapsOwnRequest_ReturnsUnprocessableEntityWithCode()
    {
        await SignInWithManagerAsync(_manager, _employee, dbContext =>
        {
            dbContext.LeaveEntitlements.Add(_entitlement);
            dbContext.LeaveRequests.Add(TestLeaveRequests.Submit(_employee, new DateOnly(2026, 6, 8), new DateOnly(2026, 6, 12)));
        });

        var response = await PostAsync(new LeaveRequestDraftRequest(new DateOnly(2026, 6, 12), new DateOnly(2026, 6, 16), LeaveType.Vacation));

        await AssertRuleViolatedAsync(response, LeaveRequestErrors.Overlapping);
    }

    [Fact]
    public async Task Submit_SpecialLeaveWithoutReason_ReturnsUnprocessableEntityWithCode()
    {
        await SignInWithManagerAsync(_manager, _employee);

        var response = await PostAsync(new LeaveRequestDraftRequest(new DateOnly(2026, 6, 8), new DateOnly(2026, 6, 8), LeaveType.SpecialLeave));

        await AssertRuleViolatedAsync(response, LeaveRequestErrors.SpecialLeaveReasonRequired);
    }

    [Fact]
    public async Task Submit_SickLeave_ReturnsValidationProblemPointingToSickLeaveEndpoint()
    {
        await SignInWithManagerAsync(_manager, _employee);

        var response = await PostAsync(new LeaveRequestDraftRequest(new DateOnly(2026, 3, 2), new DateOnly(2026, 3, 3), LeaveType.SickLeave));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(CancellationToken);
        Assert.Contains(nameof(LeaveRequestDraftRequest.Type), problem!.Errors.Keys);
    }

    [Fact]
    public async Task Submit_EndBeforeStart_ReturnsValidationProblem()
    {
        await SignInWithManagerAsync(_manager, _employee);

        var response = await PostAsync(new LeaveRequestDraftRequest(new DateOnly(2026, 6, 12), new DateOnly(2026, 6, 8), LeaveType.Vacation));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Submit_WithoutToken_ReturnsUnauthorized()
    {
        var response = await PostAsync(new LeaveRequestDraftRequest(new DateOnly(2026, 6, 8), new DateOnly(2026, 6, 8), LeaveType.Vacation));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetMyLeaveRequest_OfSomeoneElse_ReturnsNotFound()
    {
        var colleague = Employee.Create("Thomas", "Bauer", "thomas.bauer@example.at", new DateOnly(2021, 6, 15), EmployeeRole.Employee, _manager.Id);
        var foreign = TestLeaveRequests.Submit(colleague, new DateOnly(2026, 6, 8), new DateOnly(2026, 6, 8));
        await SignInWithManagerAsync(_manager, _employee, dbContext =>
        {
            dbContext.Employees.Add(colleague);
            dbContext.LeaveRequests.Add(foreign);
        });

        var response = await Client.GetAsync($"{Url}/{foreign.Id}", CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private Task<HttpResponseMessage> PostAsync(LeaveRequestDraftRequest draft) =>
        Client.PostAsJsonAsync(Url, draft, ApiJson.Options, CancellationToken);

    private static async Task AssertRuleViolatedAsync(HttpResponseMessage response, string expectedCode)
    {
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(CancellationToken);
        Assert.Equal(expectedCode, problem!.Extensions["code"]?.ToString());
    }
}
