using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using LeaveManagement.Api.Auth;
using LeaveManagement.Api.Tests.Infrastructure;
using LeaveManagement.Domain.Employees;

namespace LeaveManagement.Api.Tests.Auth;

public sealed class CurrentUserTests(ApiTestFixture fixture) : ApiTestBase(fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly Employee _employee =
        Employee.Create("Sabine", "Huber", "sabine.huber@example.at", new DateOnly(2015, 3, 1), EmployeeRole.Hr);

    [Fact]
    public async Task GetCurrentUser_SignedIn_ReturnsEmployeeWithRoleName()
    {
        await SignInAsync(_employee);

        var response = await Client.GetAsync("/api/me", CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("\"role\":\"Hr\"", await response.Content.ReadAsStringAsync(CancellationToken), StringComparison.Ordinal);
        var currentUser = await response.Content.ReadFromJsonAsync<CurrentUserResponse>(JsonOptions, CancellationToken);
        Assert.Equal(
            new CurrentUserResponse(_employee.Id, "Sabine", "Huber", "sabine.huber@example.at", EmployeeRole.Hr),
            currentUser);
    }

    [Fact]
    public async Task GetCurrentUser_WithoutToken_ReturnsUnauthorized()
    {
        var response = await Client.GetAsync("/api/me", CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetCurrentUser_ExpiredToken_ReturnsUnauthorized()
    {
        await CreateUserAsync(_employee);
        UseToken(TestTokens.Create(Factory.SigningKey, _employee.Id, DateTime.UtcNow.AddMinutes(-5)));

        var response = await Client.GetAsync("/api/me", CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetCurrentUser_TokenSignedWithOtherKey_ReturnsUnauthorized()
    {
        await CreateUserAsync(_employee);
        UseToken(TestTokens.Create(TestTokens.RandomSigningKey(), _employee.Id, DateTime.UtcNow.AddMinutes(10)));

        var response = await Client.GetAsync("/api/me", CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetCurrentUser_TokenFromOtherIssuer_ReturnsUnauthorized()
    {
        await CreateUserAsync(_employee);
        UseToken(TestTokens.Create(Factory.SigningKey, _employee.Id, DateTime.UtcNow.AddMinutes(10), issuer: "someone-else"));

        var response = await Client.GetAsync("/api/me", CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetCurrentUser_ValidHandMadeToken_ReturnsOk()
    {
        // Guards the tests above: the hand-made token is rejected only because of the changed property.
        await CreateUserAsync(_employee);
        UseToken(TestTokens.Create(Factory.SigningKey, _employee.Id, DateTime.UtcNow.AddMinutes(10)));

        var response = await Client.GetAsync("/api/me", CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private void UseToken(string token) =>
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
}
