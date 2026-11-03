using System.Net;
using System.Net.Http.Json;
using LeaveManagement.Api.Holidays;
using LeaveManagement.Api.LeaveRequests;
using LeaveManagement.Api.Tests.Infrastructure;
using LeaveManagement.Domain.Employees;
using LeaveManagement.Domain.Entitlements;
using LeaveManagement.Domain.Holidays;
using LeaveManagement.Domain.LeaveRequests;
using LeaveManagement.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LeaveManagement.Api.Tests.LeaveRequests;

public sealed class PreviewLeaveRequestTests : ApiTestBase
{
    private readonly Employee _manager;
    private readonly Employee _employee;
    private readonly LeaveEntitlement _entitlement;

    public PreviewLeaveRequestTests(ApiTestFixture fixture)
        : base(fixture)
    {
        _manager = Employee.Create("Markus", "Gruber", "markus.gruber@example.at", new DateOnly(2017, 9, 1), EmployeeRole.Manager);
        _employee = Employee.Create("Lena", "Wagner", "lena.wagner@example.at", new DateOnly(2020, 2, 1), EmployeeRole.Employee, _manager.Id);
        _entitlement = LeaveEntitlement.Create(_employee.Id, 2026);
    }

    [Fact]
    public async Task Preview_Vacation_ReturnsWorkingDaysHolidaysBalanceAndApprover()
    {
        await SignInWithManagerAsync(_manager, _employee, dbContext => dbContext.LeaveEntitlements.Add(_entitlement));

        // Ascension Day on Thursday, 14 May 2026, comes from the holidays seeded by the migration.
        var preview = await PreviewAsync(new LeaveRequestDraftRequest(new DateOnly(2026, 5, 11), new DateOnly(2026, 5, 15), LeaveType.Vacation));

        Assert.Equal(4m, preview.WorkingDays);
        Assert.Equal(
            [new HolidayResponse(new DateOnly(2026, 5, 14), "Christi Himmelfahrt", HolidayKind.Statutory)],
            preview.HolidaysInPeriod);
        Assert.Equal(21m, preview.AvailableDaysAfter);
        Assert.Null(preview.ErrorCode);
        Assert.True(preview.NeedsApproval);
        Assert.Equal("Markus Gruber", preview.ApproverName);
    }

    [Fact]
    public async Task Preview_VacationWithoutEnoughDays_ReturnsErrorCode()
    {
        await SignInWithManagerAsync(
            _manager, _employee, dbContext => dbContext.LeaveEntitlements.Add(LeaveEntitlement.Create(_employee.Id, 2026, 2m)));

        var preview = await PreviewAsync(new LeaveRequestDraftRequest(new DateOnly(2026, 6, 8), new DateOnly(2026, 6, 12), LeaveType.Vacation));

        Assert.Equal(LeaveRequestErrors.InsufficientBalance, preview.ErrorCode);
        Assert.Equal(5m, preview.WorkingDays);
        Assert.Null(preview.AvailableDaysAfter);
    }

    [Fact]
    public async Task Preview_ValidRequest_SavesNothing()
    {
        await SignInWithManagerAsync(_manager, _employee, dbContext => dbContext.LeaveEntitlements.Add(_entitlement));

        await PreviewAsync(new LeaveRequestDraftRequest(new DateOnly(2026, 6, 8), new DateOnly(2026, 6, 12), LeaveType.Vacation));

        await using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LeaveManagementDbContext>();
        Assert.Equal(0, await dbContext.LeaveRequests.CountAsync(CancellationToken));
    }

    [Fact]
    public async Task Preview_TeamMembersAway_ListsThemWithoutDetails()
    {
        var colleague = Employee.Create("Thomas", "Bauer", "thomas.bauer@example.at", new DateOnly(2021, 6, 15), EmployeeRole.Employee, _manager.Id);
        var cancelledColleague = Employee.Create("Julia", "Pichler", "julia.pichler@example.at", new DateOnly(2023, 1, 9), EmployeeRole.Employee, _manager.Id);
        var otherTeam = Employee.Create("Stefan", "Moser", "stefan.moser@example.at", new DateOnly(2019, 11, 4), EmployeeRole.Employee);
        var cancelled = TestLeaveRequests.Submit(cancelledColleague, new DateOnly(2026, 6, 8), new DateOnly(2026, 6, 9));
        cancelled.Cancel(Today);
        await SignInWithManagerAsync(_manager, _employee, dbContext =>
        {
            dbContext.Employees.AddRange(colleague, cancelledColleague, otherTeam);
            dbContext.LeaveEntitlements.Add(_entitlement);
            dbContext.LeaveRequests.AddRange(
                TestLeaveRequests.Approved(_manager, colleague, new DateOnly(2026, 6, 10), new DateOnly(2026, 6, 19)),
                TestLeaveRequests.Submit(_manager, new DateOnly(2026, 6, 12), new DateOnly(2026, 6, 12)),
                cancelled,
                TestLeaveRequests.Submit(otherTeam, new DateOnly(2026, 6, 8), new DateOnly(2026, 6, 12)));
        });

        var preview = await PreviewAsync(new LeaveRequestDraftRequest(new DateOnly(2026, 6, 8), new DateOnly(2026, 6, 12), LeaveType.Vacation));

        Assert.Equal(
            [new AbsentColleagueResponse(colleague.Id, "Thomas Bauer"), new AbsentColleagueResponse(_manager.Id, "Markus Gruber")],
            preview.AbsentColleagues);
    }

    [Fact]
    public async Task Preview_SickLeaveDuringVacationWithCertificate_ReturnsReturnedVacationDays()
    {
        await SignInWithManagerAsync(_manager, _employee, dbContext =>
        {
            dbContext.LeaveEntitlements.Add(_entitlement);
            dbContext.LeaveRequests.Add(
                TestLeaveRequests.Approved(_manager, _employee, new DateOnly(2026, 6, 8), new DateOnly(2026, 6, 19), [_entitlement]));
        });

        var preview = await PreviewAsync(new LeaveRequestDraftRequest(
            new DateOnly(2026, 6, 8), new DateOnly(2026, 6, 12), LeaveType.SickLeave, HasMedicalCertificate: true));

        Assert.Null(preview.ErrorCode);
        Assert.Equal(5m, preview.ReturnedVacationDays);
        Assert.False(preview.NeedsApproval);
        Assert.Null(preview.ApproverName);
        Assert.Null(preview.AvailableDaysAfter);
    }

    [Fact]
    public async Task Preview_LongSickLeaveDuringVacationWithoutCertificate_ReturnsErrorCode()
    {
        await SignInWithManagerAsync(_manager, _employee, dbContext =>
        {
            dbContext.LeaveEntitlements.Add(_entitlement);
            dbContext.LeaveRequests.Add(
                TestLeaveRequests.Approved(_manager, _employee, new DateOnly(2026, 6, 8), new DateOnly(2026, 6, 19), [_entitlement]));
        });

        var preview = await PreviewAsync(new LeaveRequestDraftRequest(new DateOnly(2026, 6, 8), new DateOnly(2026, 6, 12), LeaveType.SickLeave));

        Assert.Equal(LeaveRequestErrors.MedicalCertificateRequired, preview.ErrorCode);
    }

    [Theory]
    [MemberData(nameof(InvalidDrafts))]
    public async Task Preview_InvalidInput_ReturnsValidationProblem(LeaveRequestDraftRequest draft, string field)
    {
        await SignInWithManagerAsync(_manager, _employee);

        var response = await Client.PostAsJsonAsync("/api/me/leave-requests/preview", draft, ApiJson.Options, CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(CancellationToken);
        Assert.Contains(field, problem!.Errors.Keys);
    }

    public static TheoryData<LeaveRequestDraftRequest, string> InvalidDrafts() => new()
    {
        { new(new DateOnly(2026, 6, 12), new DateOnly(2026, 6, 8), LeaveType.Vacation), nameof(LeaveRequestDraftRequest.EndDate) },
        // 367 calendar days, one more than allowed.
        { new(new DateOnly(2026, 6, 8), new DateOnly(2027, 6, 9), LeaveType.Vacation), nameof(LeaveRequestDraftRequest.EndDate) },
        { new(new DateOnly(2026, 6, 8), new DateOnly(2026, 6, 8), LeaveType.Vacation, HasMedicalCertificate: true), nameof(LeaveRequestDraftRequest.HasMedicalCertificate) },
        { new(new DateOnly(2026, 6, 8), new DateOnly(2026, 6, 8), LeaveType.Vacation, new string('x', LeaveRequest.CommentMaxLength + 1)), nameof(LeaveRequestDraftRequest.Comment) },
    };

    [Fact]
    public async Task Preview_WithoutToken_ReturnsUnauthorized()
    {
        var response = await Client.PostAsJsonAsync(
            "/api/me/leave-requests/preview",
            new LeaveRequestDraftRequest(new DateOnly(2026, 6, 8), new DateOnly(2026, 6, 8), LeaveType.Vacation),
            ApiJson.Options,
            CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private async Task<LeaveRequestPreviewResponse> PreviewAsync(LeaveRequestDraftRequest draft)
    {
        var response = await Client.PostAsJsonAsync("/api/me/leave-requests/preview", draft, ApiJson.Options, CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<LeaveRequestPreviewResponse>(ApiJson.Options, CancellationToken))!;
    }
}
