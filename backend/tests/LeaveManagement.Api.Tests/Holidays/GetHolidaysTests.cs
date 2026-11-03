using System.Net;
using System.Net.Http.Json;
using LeaveManagement.Api.Holidays;
using LeaveManagement.Api.Tests.Infrastructure;
using LeaveManagement.Domain.Employees;
using LeaveManagement.Domain.Holidays;
using Microsoft.AspNetCore.Mvc;

namespace LeaveManagement.Api.Tests.Holidays;

/// <summary>Works on the holidays the migration seeds (ADR 0007); the reset keeps them.</summary>
public sealed class GetHolidaysTests(ApiTestFixture fixture) : ApiTestBase(fixture)
{
    // 13 statutory holidays plus the company half days on 24 and 31 December.
    private const int SeededHolidaysPerYear = 15;

    private readonly Employee _employee =
        Employee.Create("Lena", "Wagner", "lena.wagner@example.at", new DateOnly(2020, 2, 1), EmployeeRole.Employee);

    [Fact]
    public async Task GetHolidays_Year_ReturnsHolidaysOfThatYearSortedByDate()
    {
        await SignInAsync(_employee);

        var response = await Client.GetAsync("/api/holidays?year=2026", CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var holidays = await response.Content.ReadFromJsonAsync<List<HolidayResponse>>(ApiJson.Options, CancellationToken);
        Assert.NotNull(holidays);
        Assert.Equal(SeededHolidaysPerYear, holidays.Count);
        Assert.All(holidays, holiday => Assert.Equal(2026, holiday.Date.Year));
        Assert.Equal(holidays.OrderBy(holiday => holiday.Date), holidays);
        Assert.Equal(new HolidayResponse(new DateOnly(2026, 1, 1), "Neujahr", HolidayKind.Statutory), holidays[0]);
        Assert.Equal(new HolidayResponse(new DateOnly(2026, 12, 31), "Silvester", HolidayKind.CompanyHalfDay), holidays[^1]);
    }

    [Fact]
    public async Task GetHolidays_YearWithoutHolidays_ReturnsEmptyList()
    {
        await SignInAsync(_employee);

        var response = await Client.GetAsync("/api/holidays?year=2050", CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var holidays = await response.Content.ReadFromJsonAsync<List<HolidayResponse>>(ApiJson.Options, CancellationToken);
        Assert.Empty(holidays!);
    }

    [Fact]
    public async Task GetHolidays_YearOutOfRange_ReturnsValidationProblem()
    {
        await SignInAsync(_employee);

        var response = await Client.GetAsync("/api/holidays?year=0", CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(CancellationToken);
        Assert.Contains(nameof(GetHolidaysRequest.Year), problem!.Errors.Keys);
    }

    [Fact]
    public async Task GetHolidays_WithoutYear_ReturnsBadRequest()
    {
        await SignInAsync(_employee);

        var response = await Client.GetAsync("/api/holidays", CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetHolidays_WithoutToken_ReturnsUnauthorized()
    {
        var response = await Client.GetAsync("/api/holidays?year=2026", CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
