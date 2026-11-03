using LeaveManagement.Domain.Employees;
using LeaveManagement.Domain.LeaveRequests;
using LeaveManagement.Domain.Tests.TestData;

namespace LeaveManagement.Domain.Tests.LeaveRequests;

public sealed class MedicalCertificateRevocationTests
{
    private readonly Employee _hr = TestEmployees.Hr();
    private readonly Employee _employee = TestEmployees.ReportOf(TestEmployees.Manager());

    [Fact]
    public void Revoke_ByHr_StoresWhoWhenAndWhy()
    {
        var sickLeave = SickLeaveWithCertificate(_employee);

        sickLeave.RevokeMedicalCertificate(_hr, "  Attest nicht lesbar  ", TestRequests.Now);

        Assert.True(sickLeave.IsCertificateRevoked);
        Assert.Equal(_hr.Id, sickLeave.CertificateRevokedById);
        Assert.Equal(TestRequests.Now, sickLeave.CertificateRevokedAt);
        Assert.Equal(TimeSpan.Zero, sickLeave.CertificateRevokedAt!.Value.Offset);
        Assert.Equal("Attest nicht lesbar", sickLeave.CertificateRevocationReason);
        Assert.Equal(LeaveRequestStatus.Approved, sickLeave.Status);
    }

    [Theory]
    [InlineData(EmployeeRole.Employee)]
    [InlineData(EmployeeRole.Manager)]
    public void Revoke_ByOtherRole_Throws(EmployeeRole role)
    {
        var sickLeave = SickLeaveWithCertificate(_employee);
        var notHr = Employee.Create("Max", "Muster", "max.muster@example.at", new DateOnly(2018, 1, 1), role);

        AssertRule.Violated(
            LeaveRequestErrors.NotHr,
            () => sickLeave.RevokeMedicalCertificate(notHr, "Grund", TestRequests.Now));
    }

    [Fact]
    public void Revoke_OwnSickLeave_Throws()
    {
        var sickLeave = SickLeaveWithCertificate(_hr);

        AssertRule.Violated(
            LeaveRequestErrors.OwnRequest,
            () => sickLeave.RevokeMedicalCertificate(_hr, "Grund", TestRequests.Now));
    }

    [Fact]
    public void Revoke_WithoutCertificate_Throws()
    {
        var sickLeave = TestRequests.Submit(
            _employee, new DateOnly(2026, 2, 23), new DateOnly(2026, 2, 24), LeaveType.SickLeave);

        AssertRule.Violated(
            LeaveRequestErrors.NoMedicalCertificate,
            () => sickLeave.RevokeMedicalCertificate(_hr, "Grund", TestRequests.Now));
    }

    [Fact]
    public void Revoke_Twice_Throws()
    {
        var sickLeave = SickLeaveWithCertificate(_employee);
        sickLeave.RevokeMedicalCertificate(_hr, "Grund", TestRequests.Now);

        AssertRule.Violated(
            LeaveRequestErrors.CertificateAlreadyRevoked,
            () => sickLeave.RevokeMedicalCertificate(_hr, "Noch einmal", TestRequests.Now));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Revoke_WithoutReason_Throws(string reason)
    {
        var sickLeave = SickLeaveWithCertificate(_employee);

        Assert.Throws<ArgumentException>(() => sickLeave.RevokeMedicalCertificate(_hr, reason, TestRequests.Now));
    }

    [Fact]
    public void Revoke_ReasonTooLong_Throws()
    {
        var sickLeave = SickLeaveWithCertificate(_employee);
        var reason = new string('x', LeaveRequest.RevocationReasonMaxLength + 1);

        Assert.Throws<ArgumentException>(() => sickLeave.RevokeMedicalCertificate(_hr, reason, TestRequests.Now));
    }

    private static LeaveRequest SickLeaveWithCertificate(Employee employee) =>
        TestRequests.Submit(
            employee,
            new DateOnly(2026, 2, 16),
            new DateOnly(2026, 2, 20),
            LeaveType.SickLeave,
            hasMedicalCertificate: true);
}
