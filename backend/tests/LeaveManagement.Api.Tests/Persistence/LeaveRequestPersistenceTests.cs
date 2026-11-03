using LeaveManagement.Api.Tests.Infrastructure;
using LeaveManagement.Domain.Common;
using LeaveManagement.Domain.Employees;
using LeaveManagement.Domain.Entitlements;
using LeaveManagement.Domain.Holidays;
using LeaveManagement.Domain.LeaveRequests;
using LeaveManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LeaveManagement.Api.Tests.Persistence;

/// <summary>Checks the EF Core mapping of the leave domain against the real database schema.</summary>
public sealed class LeaveRequestPersistenceTests : ApiTestBase
{
    private static readonly DateOnly Today = new(2026, 3, 2);
    private static readonly DateTimeOffset Now = new(2026, 3, 2, 8, 0, 0, TimeSpan.FromHours(1));

    private readonly Employee _manager;
    private readonly Employee _employee;

    public LeaveRequestPersistenceTests(ApiTestFixture fixture)
        : base(fixture)
    {
        _manager = Employee.Create("Markus", "Gruber", "markus.gruber@example.at", new DateOnly(2017, 9, 1), EmployeeRole.Manager);
        _employee = Employee.Create("Lena", "Wagner", "lena.wagner@example.at", new DateOnly(2020, 2, 1), EmployeeRole.Employee, _manager.Id);
    }

    [Fact]
    public async Task Holidays_AfterMigration_ContainStatutoryHolidaysAndCompanyHalfDays()
    {
        await using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LeaveManagementDbContext>();

        var holidays2026 = await dbContext.Holidays
            .Where(holiday => holiday.Date.Year == 2026)
            .ToListAsync(CancellationToken);

        Assert.Equal(13, holidays2026.Count(holiday => holiday.Kind == HolidayKind.Statutory));
        Assert.Equal(
            [new DateOnly(2026, 12, 24), new DateOnly(2026, 12, 31)],
            holidays2026.Where(holiday => holiday.Kind == HolidayKind.CompanyHalfDay).Select(holiday => holiday.Date).Order());
    }

    [Fact]
    public async Task SaveChanges_LeaveRequest_RoundTripsWithDays()
    {
        var request = await SubmitAndSaveVacationAsync(new DateOnly(2026, 5, 11), new DateOnly(2026, 5, 15));

        await using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LeaveManagementDbContext>();
        var loaded = await dbContext.LeaveRequests.SingleAsync(entry => entry.Id == request.Id, CancellationToken);

        Assert.Equal(_employee.Id, loaded.EmployeeId);
        Assert.Equal(new DateRange(new DateOnly(2026, 5, 11), new DateOnly(2026, 5, 15)), loaded.Period);
        Assert.Equal(LeaveType.Vacation, loaded.Type);
        Assert.Equal(LeaveRequestStatus.Pending, loaded.Status);
        Assert.Equal(4m, loaded.WorkingDays);
        Assert.Equal("Sommerurlaub", loaded.Comment);
        Assert.Equal(Now, loaded.CreatedAt);
        Assert.NotEqual(0u, loaded.Version);

        // Ascension Day (14 May 2026) comes from the holidays seeded by the migration.
        Assert.Equal(
            [new DateOnly(2026, 5, 11), new DateOnly(2026, 5, 12), new DateOnly(2026, 5, 13), new DateOnly(2026, 5, 15)],
            loaded.Days.Select(day => day.Date).Order());
    }

    [Fact]
    public async Task SaveChanges_SickLeaveWithCertificate_RoundTripsCertificateFlag()
    {
        var sickLeave = SickLeaveWithCertificate();
        await ArrangeAsync(dbContext =>
        {
            dbContext.Employees.AddRange(_manager, _employee);
            dbContext.LeaveRequests.Add(sickLeave);
        });

        await using var scope = CreateScope();
        var loaded = await scope.ServiceProvider.GetRequiredService<LeaveManagementDbContext>()
            .LeaveRequests.SingleAsync(entry => entry.Id == sickLeave.Id, CancellationToken);

        Assert.True(loaded.HasMedicalCertificate);
    }

    [Fact]
    public async Task SaveChanges_MedicalCertificate_RoundTripsFile()
    {
        var sickLeave = SickLeaveWithCertificate();
        byte[] content = [0x25, 0x50, 0x44, 0x46, 0x2D, 0x31];
        var certificate = MedicalCertificate.Create(sickLeave, "attest.pdf", "application/pdf", content, Now);
        await ArrangeAsync(dbContext =>
        {
            dbContext.Employees.AddRange(_manager, _employee);
            dbContext.LeaveRequests.Add(sickLeave);
            dbContext.MedicalCertificates.Add(certificate);
        });

        await using var scope = CreateScope();
        var loaded = await scope.ServiceProvider.GetRequiredService<LeaveManagementDbContext>()
            .MedicalCertificates.SingleAsync(entry => entry.LeaveRequestId == sickLeave.Id, CancellationToken);

        Assert.Equal("attest.pdf", loaded.FileName);
        Assert.Equal("application/pdf", loaded.ContentType);
        Assert.Equal(content.Length, loaded.SizeInBytes);
        Assert.Equal(content, loaded.Content);
        Assert.Equal(Now, loaded.UploadedAt);
    }

    [Fact]
    public async Task SaveChanges_SecondCertificateForSameSickLeave_IsRejectedByDatabase()
    {
        var sickLeave = SickLeaveWithCertificate();
        await ArrangeAsync(dbContext =>
        {
            dbContext.Employees.AddRange(_manager, _employee);
            dbContext.LeaveRequests.Add(sickLeave);
            dbContext.MedicalCertificates.Add(MedicalCertificate.Create(sickLeave, "a.pdf", "application/pdf", [1], Now));
        });

        await Assert.ThrowsAsync<DbUpdateException>(() => ArrangeAsync(dbContext =>
            dbContext.MedicalCertificates.Add(MedicalCertificate.Create(sickLeave, "b.pdf", "application/pdf", [2], Now))));
    }

    [Fact]
    public async Task SaveChanges_RevokedCertificate_RoundTripsRevocation()
    {
        var hr = Employee.Create("Sabine", "Huber", "sabine.huber@example.at", new DateOnly(2015, 3, 1), EmployeeRole.Hr);
        var sickLeave = SickLeaveWithCertificate();
        sickLeave.RevokeMedicalCertificate(hr, "Attest nicht lesbar", Now);
        await ArrangeAsync(dbContext =>
        {
            dbContext.Employees.AddRange(_manager, _employee, hr);
            dbContext.LeaveRequests.Add(sickLeave);
        });

        await using var scope = CreateScope();
        var loaded = await scope.ServiceProvider.GetRequiredService<LeaveManagementDbContext>()
            .LeaveRequests.SingleAsync(entry => entry.Id == sickLeave.Id, CancellationToken);

        Assert.True(loaded.IsCertificateRevoked);
        Assert.Equal(Now, loaded.CertificateRevokedAt);
        Assert.Equal(hr.Id, loaded.CertificateRevokedById);
        Assert.Equal("Attest nicht lesbar", loaded.CertificateRevocationReason);
    }

    [Fact]
    public async Task SaveChanges_ConcurrentDecisions_ThrowsConcurrencyException()
    {
        var request = await SubmitAndSaveVacationAsync(new DateOnly(2026, 6, 8), new DateOnly(2026, 6, 12));

        await using var firstScope = CreateScope();
        await using var secondScope = CreateScope();
        var firstContext = firstScope.ServiceProvider.GetRequiredService<LeaveManagementDbContext>();
        var secondContext = secondScope.ServiceProvider.GetRequiredService<LeaveManagementDbContext>();
        var firstCopy = await firstContext.LeaveRequests.SingleAsync(entry => entry.Id == request.Id, CancellationToken);
        var secondCopy = await secondContext.LeaveRequests.SingleAsync(entry => entry.Id == request.Id, CancellationToken);

        firstCopy.Approve(_manager, _employee, Now);
        await firstContext.SaveChangesAsync(CancellationToken);

        secondCopy.Reject(_manager, _employee, Now);
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => secondContext.SaveChangesAsync(CancellationToken));
    }

    [Fact]
    public async Task SaveChanges_SecondEntitlementForSameYear_IsRejectedByDatabase()
    {
        await ArrangeAsync(dbContext =>
        {
            dbContext.Employees.AddRange(_manager, _employee);
            dbContext.LeaveEntitlements.Add(LeaveEntitlement.Create(_employee.Id, 2026));
        });

        await Assert.ThrowsAsync<DbUpdateException>(() => ArrangeAsync(dbContext =>
            dbContext.LeaveEntitlements.Add(LeaveEntitlement.Create(_employee.Id, 2026, 5m))));
    }

    private LeaveRequest SickLeaveWithCertificate() =>
        LeaveRequest.Submit(
            _employee,
            new LeaveRequestDraft(
                new DateRange(new DateOnly(2026, 2, 23), new DateOnly(2026, 2, 27)),
                LeaveType.SickLeave,
                HasMedicalCertificate: true),
            new SubmissionContext(new WorkingDayCalendar([]), [], [], Today, Now));

    private async Task<LeaveRequest> SubmitAndSaveVacationAsync(DateOnly start, DateOnly end)
    {
        await using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LeaveManagementDbContext>();
        var calendar = new WorkingDayCalendar(await dbContext.Holidays.ToListAsync(CancellationToken));
        var entitlement = LeaveEntitlement.Create(_employee.Id, 2026);

        var request = LeaveRequest.Submit(
            _employee,
            new LeaveRequestDraft(new DateRange(start, end), LeaveType.Vacation, "Sommerurlaub"),
            new SubmissionContext(calendar, [], [entitlement], Today, Now));

        dbContext.Employees.AddRange(_manager, _employee);
        dbContext.LeaveEntitlements.Add(entitlement);
        dbContext.LeaveRequests.Add(request);
        await dbContext.SaveChangesAsync(CancellationToken);

        return request;
    }
}
