using System.Net;
using System.Net.Http.Json;
using LeaveManagement.Api.LeaveRequests;
using LeaveManagement.Api.Tests.Infrastructure;
using LeaveManagement.Domain.Employees;
using LeaveManagement.Domain.LeaveRequests;

namespace LeaveManagement.Api.Tests.LeaveRequests;

public sealed class GetSpecialLeaveReasonsTests(ApiTestFixture fixture) : ApiTestBase(fixture)
{
    private readonly Employee _employee =
        Employee.Create("Lena", "Wagner", "lena.wagner@example.at", new DateOnly(2020, 2, 1), EmployeeRole.Employee);

    [Fact]
    public async Task GetSpecialLeaveReasons_SignedIn_ReturnsEveryReasonWithItsLimitInEnumOrder()
    {
        await SignInAsync(_employee);

        var response = await Client.GetAsync("/api/special-leave-reasons", CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var reasons = await response.Content.ReadFromJsonAsync<List<SpecialLeaveReasonResponse>>(ApiJson.Options, CancellationToken);
        Assert.NotNull(reasons);
        Assert.Equal(Enum.GetValues<SpecialLeaveReason>(), reasons.Select(reason => reason.Reason));
        Assert.Contains(new SpecialLeaveReasonResponse(SpecialLeaveReason.Relocation, 2m, SpecialLeaveLimitPeriod.Occasion), reasons);
        Assert.Contains(new SpecialLeaveReasonResponse(SpecialLeaveReason.CareLeave, 5m, SpecialLeaveLimitPeriod.WorkingYear), reasons);
    }

    [Fact]
    public async Task GetSpecialLeaveReasons_WithoutToken_ReturnsUnauthorized()
    {
        var response = await Client.GetAsync("/api/special-leave-reasons", CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
