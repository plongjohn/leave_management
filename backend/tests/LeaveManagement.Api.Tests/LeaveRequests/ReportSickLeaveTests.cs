using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
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

public sealed class ReportSickLeaveTests : ApiTestBase
{
    private static readonly byte[] Pdf = [.. "%PDF-1.7 test"u8];

    private readonly Employee _manager;
    private readonly Employee _employee;
    private readonly LeaveEntitlement _entitlement;
    private readonly LeaveRequest _vacation;

    public ReportSickLeaveTests(ApiTestFixture fixture)
        : base(fixture)
    {
        _manager = Employee.Create("Markus", "Gruber", "markus.gruber@example.at", new DateOnly(2017, 9, 1), EmployeeRole.Manager);
        _employee = Employee.Create("Lena", "Wagner", "lena.wagner@example.at", new DateOnly(2020, 2, 1), EmployeeRole.Employee, _manager.Id);
        _entitlement = LeaveEntitlement.Create(_employee.Id, 2026);

        // Approved vacation from Monday, 8 June, to Friday, 19 June 2026.
        _vacation = TestLeaveRequests.Approved(_manager, _employee, new DateOnly(2026, 6, 8), new DateOnly(2026, 6, 19), [_entitlement]);
    }

    [Fact]
    public async Task Report_ShortSickLeaveWithoutCertificate_CreatesApprovedSickLeave()
    {
        await SignInWithVacationAsync();

        var response = await ReportAsync(new DateOnly(2026, 3, 2), new DateOnly(2026, 3, 4));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<MyLeaveRequestResponse>(ApiJson.Options, CancellationToken);
        Assert.Equal(LeaveType.SickLeave, created!.Type);
        Assert.Equal(LeaveRequestStatus.Approved, created.Status);
        Assert.False(created.HasMedicalCertificate);
    }

    [Fact]
    public async Task Report_LongSickLeaveDuringVacationWithCertificate_StoresCertificateWithDetectedType()
    {
        await SignInWithVacationAsync();

        // The client claims a JPEG named attest.jpg, but the content is a PDF.
        var response = await ReportAsync(
            new DateOnly(2026, 6, 8), new DateOnly(2026, 6, 12), Certificate(Pdf, "attest.jpg", "image/jpeg"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<MyLeaveRequestResponse>(ApiJson.Options, CancellationToken);
        Assert.True(created!.HasMedicalCertificate);

        await using var scope = CreateScope();
        var certificate = await scope.ServiceProvider.GetRequiredService<LeaveManagementDbContext>()
            .MedicalCertificates.SingleAsync(entry => entry.LeaveRequestId == created.Id, CancellationToken);
        Assert.Equal("application/pdf", certificate.ContentType);
        Assert.Equal("attest.jpg", certificate.FileName);
        Assert.Equal(Pdf, certificate.Content);
    }

    [Fact]
    public async Task Report_LongSickLeaveDuringVacationWithoutCertificate_ReturnsUnprocessableEntityAndSavesNothing()
    {
        await SignInWithVacationAsync();

        var response = await ReportAsync(new DateOnly(2026, 6, 8), new DateOnly(2026, 6, 12));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(CancellationToken);
        Assert.Equal(LeaveRequestErrors.MedicalCertificateRequired, problem!.Extensions["code"]?.ToString());
        await using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LeaveManagementDbContext>();
        Assert.Equal(1, await dbContext.LeaveRequests.CountAsync(CancellationToken));
    }

    [Theory]
    [InlineData("<html>not a pdf</html>")]
    [InlineData("MZ executable")]
    public async Task Report_CertificateIsNoPdfOrImage_ReturnsValidationProblem(string content)
    {
        await SignInWithVacationAsync();

        var response = await ReportAsync(
            new DateOnly(2026, 6, 8),
            new DateOnly(2026, 6, 12),
            Certificate([.. System.Text.Encoding.UTF8.GetBytes(content)], "attest.pdf", "application/pdf"));

        await AssertValidationProblemAsync(response, nameof(ReportSickLeaveForm.Certificate));
    }

    [Fact]
    public async Task Report_CertificateTooLarge_ReturnsValidationProblem()
    {
        await SignInWithVacationAsync();
        var tooLarge = new byte[MedicalCertificate.MaxSizeInBytes + 1];
        Pdf.CopyTo(tooLarge, 0);

        var response = await ReportAsync(
            new DateOnly(2026, 6, 8), new DateOnly(2026, 6, 12), Certificate(tooLarge, "attest.pdf", "application/pdf"));

        await AssertValidationProblemAsync(response, nameof(ReportSickLeaveForm.Certificate));
    }

    [Fact]
    public async Task Report_EndBeforeStart_ReturnsValidationProblem()
    {
        await SignInWithVacationAsync();

        var response = await ReportAsync(new DateOnly(2026, 3, 4), new DateOnly(2026, 3, 2));

        await AssertValidationProblemAsync(response, nameof(ReportSickLeaveForm.EndDate));
    }

    [Fact]
    public async Task Report_WithoutToken_ReturnsUnauthorized()
    {
        var response = await ReportAsync(new DateOnly(2026, 3, 2), new DateOnly(2026, 3, 4));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetCertificate_OwnSickLeave_ReturnsFileThatIsNeitherCachedNorSniffed()
    {
        await SignInWithVacationAsync();
        var id = await ReportWithCertificateAsync();

        var response = await Client.GetAsync($"/api/leave-requests/{id}/medical-certificate", CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/pdf", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("attest.pdf", response.Content.Headers.ContentDisposition?.FileName);
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.True(response.Headers.CacheControl?.NoStore);
        Assert.Equal(Pdf, await response.Content.ReadAsByteArrayAsync(CancellationToken));
    }

    [Fact]
    public async Task GetCertificate_AsHr_ReturnsFile()
    {
        await SignInWithVacationAsync();
        var id = await ReportWithCertificateAsync();

        await SignInAsync(Employee.Create("Sabine", "Huber", "sabine.huber@example.at", new DateOnly(2015, 3, 1), EmployeeRole.Hr));
        var response = await Client.GetAsync($"/api/leave-requests/{id}/medical-certificate", CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetCertificate_AsManager_ReturnsNotFound()
    {
        await SignInWithVacationAsync();
        var id = await ReportWithCertificateAsync();

        await SignInAsExistingAsync(_manager);
        var response = await Client.GetAsync($"/api/leave-requests/{id}/medical-certificate", CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetCertificate_SickLeaveWithoutCertificate_ReturnsNotFound()
    {
        await SignInWithVacationAsync();
        var response = await ReportAsync(new DateOnly(2026, 3, 2), new DateOnly(2026, 3, 4));
        var created = await response.Content.ReadFromJsonAsync<MyLeaveRequestResponse>(ApiJson.Options, CancellationToken);

        var download = await Client.GetAsync($"/api/leave-requests/{created!.Id}/medical-certificate", CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, download.StatusCode);
    }

    private Task SignInWithVacationAsync() =>
        SignInWithManagerAsync(_manager, _employee, dbContext =>
        {
            dbContext.LeaveEntitlements.Add(_entitlement);
            dbContext.LeaveRequests.Add(_vacation);
        });

    private async Task<Guid> ReportWithCertificateAsync()
    {
        var response = await ReportAsync(
            new DateOnly(2026, 6, 8), new DateOnly(2026, 6, 12), Certificate(Pdf, "attest.pdf", "application/pdf"));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<MyLeaveRequestResponse>(ApiJson.Options, CancellationToken))!.Id;
    }

    private static ByteArrayContent Certificate(byte[] content, string fileName, string contentType)
    {
        var file = new ByteArrayContent(content);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        file.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
        {
            Name = "certificate",
            FileName = fileName,
        };
        return file;
    }

    private async Task<HttpResponseMessage> ReportAsync(DateOnly start, DateOnly end, ByteArrayContent? certificate = null)
    {
        using var form = new MultipartFormDataContent
        {
            { new StringContent(start.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)), "startDate" },
            { new StringContent(end.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)), "endDate" },
        };
        if (certificate is not null)
        {
            form.Add(certificate);
        }

        return await Client.PostAsync("/api/me/sick-leaves", form, CancellationToken);
    }

    private static async Task AssertValidationProblemAsync(HttpResponseMessage response, string field)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(CancellationToken);
        Assert.Contains(field, problem!.Errors.Keys);
    }
}
