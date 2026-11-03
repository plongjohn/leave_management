using LeaveManagement.Domain.Employees;
using LeaveManagement.Domain.LeaveRequests;
using LeaveManagement.Domain.Tests.TestData;

namespace LeaveManagement.Domain.Tests.LeaveRequests;

/// <summary>Which sick leave may be reported during vacation and when a certificate is needed.</summary>
public sealed class SickLeaveDuringVacationSubmitTests
{
    // Approved vacation from Monday, 8 June, to Friday, 19 June 2026.
    private static readonly DateOnly VacationStart = new(2026, 6, 8);
    private static readonly DateOnly VacationEnd = new(2026, 6, 19);

    private readonly Employee _manager = TestEmployees.Manager();
    private readonly Employee _employee;
    private readonly LeaveRequest _vacation;

    public SickLeaveDuringVacationSubmitTests()
    {
        _employee = TestEmployees.ReportOf(_manager);
        _vacation = TestRequests.Approved(_manager, _employee, VacationStart, VacationEnd);
    }

    [Fact]
    public void Submit_ThreeCalendarDaysWithoutCertificate_IsApproved()
    {
        var sickLeave = ReportSickLeave(new DateOnly(2026, 6, 9), new DateOnly(2026, 6, 11));

        Assert.Equal(LeaveRequestStatus.Approved, sickLeave.Status);
        Assert.False(sickLeave.HasMedicalCertificate);
    }

    [Fact]
    public void Submit_MoreThanThreeCalendarDaysWithoutCertificate_Throws()
    {
        AssertRule.Violated(
            LeaveRequestErrors.MedicalCertificateRequired,
            () => ReportSickLeave(new DateOnly(2026, 6, 9), new DateOnly(2026, 6, 12)));
    }

    [Fact]
    public void Submit_FridayToMonday_CountsCalendarDaysNotWorkingDays()
    {
        // Two working days, but four calendar days: a certificate is needed.
        AssertRule.Violated(
            LeaveRequestErrors.MedicalCertificateRequired,
            () => ReportSickLeave(new DateOnly(2026, 6, 12), new DateOnly(2026, 6, 15)));
    }

    [Fact]
    public void Submit_MoreThanThreeCalendarDaysWithCertificate_IsApprovedWithCertificate()
    {
        var sickLeave = ReportSickLeave(new DateOnly(2026, 6, 9), new DateOnly(2026, 6, 15), hasMedicalCertificate: true);

        Assert.Equal(LeaveRequestStatus.Approved, sickLeave.Status);
        Assert.True(sickLeave.HasMedicalCertificate);
    }

    [Fact]
    public void Submit_StartsBeforeVacation_CountsWholeSickLeave()
    {
        // Only Monday and Tuesday lie in the vacation, but the sickness lasts five calendar days.
        AssertRule.Violated(
            LeaveRequestErrors.MedicalCertificateRequired,
            () => ReportSickLeave(new DateOnly(2026, 6, 5), new DateOnly(2026, 6, 9)));
    }

    [Fact]
    public void Submit_OverlapsPendingVacation_Throws()
    {
        var pendingVacation = TestRequests.Submit(_employee, new DateOnly(2026, 7, 6), new DateOnly(2026, 7, 10));

        AssertRule.Violated(
            LeaveRequestErrors.OverlapsPendingVacation,
            () => TestRequests.Submit(
                _employee,
                new DateOnly(2026, 7, 7),
                new DateOnly(2026, 7, 8),
                LeaveType.SickLeave,
                existingRequests: [pendingVacation]));
    }

    [Theory]
    [InlineData(LeaveType.SickLeave)]
    [InlineData(LeaveType.TimeOffInLieu)]
    public void Submit_OverlapsOtherApprovedRequest_Throws(LeaveType otherType)
    {
        var other = TestRequests.Submit(_employee, new DateOnly(2026, 7, 6), new DateOnly(2026, 7, 7), otherType);
        if (other.Status == LeaveRequestStatus.Pending)
        {
            other.Approve(_manager, _employee, TestRequests.Now);
        }

        AssertRule.Violated(
            LeaveRequestErrors.Overlapping,
            () => TestRequests.Submit(
                _employee,
                new DateOnly(2026, 7, 7),
                new DateOnly(2026, 7, 8),
                LeaveType.SickLeave,
                existingRequests: [other]));
    }

    [Fact]
    public void Submit_VacationOverlappingSickLeave_Throws()
    {
        var sickLeave = TestRequests.Submit(_employee, new DateOnly(2026, 7, 6), new DateOnly(2026, 7, 7), LeaveType.SickLeave);

        AssertRule.Violated(
            LeaveRequestErrors.Overlapping,
            () => TestRequests.Submit(
                _employee, new DateOnly(2026, 7, 7), new DateOnly(2026, 7, 10), existingRequests: [sickLeave]));
    }

    [Fact]
    public void Submit_SickLeaveOutsideVacationWithCertificate_KeepsCertificate()
    {
        var sickLeave = TestRequests.Submit(
            _employee,
            new DateOnly(2026, 7, 6),
            new DateOnly(2026, 7, 10),
            LeaveType.SickLeave,
            existingRequests: [_vacation],
            hasMedicalCertificate: true);

        Assert.True(sickLeave.HasMedicalCertificate);
    }

    [Fact]
    public void Submit_SickLeaveOutsideVacationWithoutCertificate_IsApproved()
    {
        var sickLeave = TestRequests.Submit(
            _employee, new DateOnly(2026, 7, 6), new DateOnly(2026, 7, 10), LeaveType.SickLeave, existingRequests: [_vacation]);

        Assert.Equal(LeaveRequestStatus.Approved, sickLeave.Status);
    }

    [Theory]
    [InlineData(LeaveType.Vacation)]
    [InlineData(LeaveType.TimeOffInLieu)]
    public void Submit_CertificateForOtherType_Throws(LeaveType type)
    {
        Assert.Throws<ArgumentException>(() => TestRequests.Submit(
            _employee, new DateOnly(2026, 7, 6), new DateOnly(2026, 7, 7), type, hasMedicalCertificate: true));
    }

    private LeaveRequest ReportSickLeave(DateOnly start, DateOnly end, bool hasMedicalCertificate = false) =>
        TestRequests.Submit(
            _employee,
            start,
            end,
            LeaveType.SickLeave,
            existingRequests: [_vacation],
            hasMedicalCertificate: hasMedicalCertificate);
}
