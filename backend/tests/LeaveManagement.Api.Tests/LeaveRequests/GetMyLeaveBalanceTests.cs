using System.Net;
using System.Net.Http.Json;
using LeaveManagement.Api.LeaveRequests;
using LeaveManagement.Api.Tests.Infrastructure;
using LeaveManagement.Domain.Employees;
using LeaveManagement.Domain.Entitlements;
using LeaveManagement.Domain.LeaveRequests;

namespace LeaveManagement.Api.Tests.LeaveRequests;

public sealed class GetMyLeaveBalanceTests : ApiTestBase
{
    private readonly Employee _manager;
    private readonly Employee _employee;

    public GetMyLeaveBalanceTests(ApiTestFixture fixture)
        : base(fixture)
    {
        _manager = Employee.Create("Markus", "Gruber", "markus.gruber@example.at", new DateOnly(2017, 9, 1), EmployeeRole.Manager);
        _employee = Employee.Create("Lena", "Wagner", "lena.wagner@example.at", new DateOnly(2020, 2, 1), EmployeeRole.Employee, _manager.Id);
    }

    [Fact]
    public async Task GetMyLeaveBalance_Entitlements_ReturnsYearsNotExpiredAndAvailableDays()
    {
        // 2023 expired on 31 December 2025; days are taken from 2025 first.
        List<LeaveEntitlement> entitlements =
        [
            LeaveEntitlement.Create(_employee.Id, 2023),
            LeaveEntitlement.Create(_employee.Id, 2025),
            LeaveEntitlement.Create(_employee.Id, 2026),
        ];
        var approved = TestLeaveRequests.Approved(_manager, _employee, new DateOnly(2026, 6, 8), new DateOnly(2026, 6, 12), entitlements);
        var pending = TestLeaveRequests.Submit(
            _employee, new DateOnly(2026, 8, 3), new DateOnly(2026, 8, 5), existingRequests: [approved], entitlements: entitlements);
        await SignInWithDataAsync(entitlements, approved, pending);

        var balance = await GetAsync();

        Assert.Equal(
            [
                new EntitlementYearResponse(2025, 25m, 5m, 3m, 17m, new DateOnly(2027, 12, 31)),
                new EntitlementYearResponse(2026, 25m, 0m, 0m, 25m, new DateOnly(2028, 12, 31)),
            ],
            balance.Years);
        Assert.Equal(42m, balance.AvailableDays);
    }

    [Fact]
    public async Task GetMyLeaveBalance_SickLeaveDuringVacation_DoesNotTakeSickDays()
    {
        List<LeaveEntitlement> entitlements = [LeaveEntitlement.Create(_employee.Id, 2026)];
        var vacation = TestLeaveRequests.Approved(_manager, _employee, new DateOnly(2026, 6, 8), new DateOnly(2026, 6, 19), entitlements);
        var sickLeave = TestLeaveRequests.Submit(
            _employee, new DateOnly(2026, 6, 8), new DateOnly(2026, 6, 10), LeaveType.SickLeave, existingRequests: [vacation]);
        await SignInWithDataAsync(entitlements, vacation, sickLeave);

        var balance = await GetAsync();

        Assert.Equal(7m, Assert.Single(balance.Years).ApprovedDays);
        Assert.Equal(18m, balance.AvailableDays);
    }

    [Fact]
    public async Task GetMyLeaveBalance_RevokedCertificateLeavesDaysUncovered_ReturnsNegativeAvailableDays()
    {
        // Ten days of entitlement: the returned days are planned again, then HR revokes the certificate.
        var hr = Employee.Create("Sabine", "Huber", "sabine.huber@example.at", new DateOnly(2015, 3, 1), EmployeeRole.Hr);
        List<LeaveEntitlement> entitlements = [LeaveEntitlement.Create(_employee.Id, 2026, 10m)];
        var vacation = TestLeaveRequests.Approved(_manager, _employee, new DateOnly(2026, 6, 8), new DateOnly(2026, 6, 19), entitlements);
        var sickLeave = TestLeaveRequests.Submit(
            _employee,
            new DateOnly(2026, 6, 8),
            new DateOnly(2026, 6, 16),
            LeaveType.SickLeave,
            existingRequests: [vacation],
            hasMedicalCertificate: true);
        var laterVacation = TestLeaveRequests.Submit(
            _employee,
            new DateOnly(2026, 9, 7),
            new DateOnly(2026, 9, 15),
            existingRequests: [vacation, sickLeave],
            entitlements: entitlements);
        sickLeave.RevokeMedicalCertificate(hr, "Attest nicht lesbar", TestLeaveRequests.Now);
        await ArrangeAsync(dbContext => dbContext.Employees.Add(hr));
        await SignInWithDataAsync(entitlements, vacation, sickLeave, laterVacation);

        var balance = await GetAsync();

        Assert.Equal(-4m, balance.AvailableDays);
    }

    [Fact]
    public async Task GetMyLeaveBalance_WithoutToken_ReturnsUnauthorized()
    {
        var response = await Client.GetAsync("/api/me/leave-balance", CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private async Task SignInWithDataAsync(List<LeaveEntitlement> entitlements, params LeaveRequest[] requests)
    {
        await ArrangeAsync(dbContext => dbContext.Employees.Add(_manager));
        await SignInAsync(_employee);
        await ArrangeAsync(dbContext =>
        {
            dbContext.LeaveEntitlements.AddRange(entitlements);
            dbContext.LeaveRequests.AddRange(requests);
        });
    }

    private async Task<LeaveBalanceResponse> GetAsync()
    {
        var response = await Client.GetAsync("/api/me/leave-balance", CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<LeaveBalanceResponse>(ApiJson.Options, CancellationToken))!;
    }
}
