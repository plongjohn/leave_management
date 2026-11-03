using LeaveManagement.Domain.Employees;
using LeaveManagement.Domain.Entitlements;
using LeaveManagement.Domain.LeaveRequests;
using LeaveManagement.Domain.Tests.TestData;

namespace LeaveManagement.Domain.Tests.LeaveRequests;

/// <summary>How sick leave during vacation and the revocation of its certificate change the balance.</summary>
public sealed class SickLeaveDuringVacationBalanceTests
{
    // Approved vacation from Monday, 8 June, to Friday, 19 June 2026: ten working days.
    private static readonly DateOnly VacationStart = new(2026, 6, 8);
    private static readonly DateOnly VacationEnd = new(2026, 6, 19);

    private readonly Employee _manager = TestEmployees.Manager();
    private readonly Employee _hr = TestEmployees.Hr();
    private readonly Employee _employee;
    private readonly LeaveRequest _vacation;

    public SickLeaveDuringVacationBalanceTests()
    {
        _employee = TestEmployees.ReportOf(_manager);
        _vacation = TestRequests.Approved(_manager, _employee, VacationStart, VacationEnd);
    }

    [Fact]
    public void Calculate_ShortSickLeaveWithoutCertificate_ReturnsSickDays()
    {
        var sickLeave = ReportSickLeave(new DateOnly(2026, 6, 9), new DateOnly(2026, 6, 11));

        Assert.Equal(7m, TakenDays(_vacation, sickLeave));
    }

    [Fact]
    public void Calculate_RevokedCertificate_CountsDaysAfterThirdWorkingDayAsVacationAgain()
    {
        // Seven working days sick; Monday to Wednesday stay sickness, the four days after count as vacation.
        var sickLeave = ReportSickLeave(new DateOnly(2026, 6, 8), new DateOnly(2026, 6, 16), hasMedicalCertificate: true);

        sickLeave.RevokeMedicalCertificate(_hr, "Attest nicht lesbar", TestRequests.Now);

        Assert.Equal(3m, SickLeaveDuringVacation.ReturnedDays(_vacation, [_vacation, sickLeave]));
        Assert.Equal(7m, TakenDays(_vacation, sickLeave));
    }

    [Fact]
    public void Calculate_RevokedCertificateThursdayToTuesday_CountsOnlyTuesdayAsVacationAgain()
    {
        // Thursday, Friday and Monday are the three working days without certificate; the weekend does not count.
        var sickLeave = ReportSickLeave(new DateOnly(2026, 6, 11), new DateOnly(2026, 6, 16), hasMedicalCertificate: true);

        sickLeave.RevokeMedicalCertificate(_hr, "Kein gültiges Attest", TestRequests.Now);

        Assert.Equal(3m, SickLeaveDuringVacation.ReturnedDays(_vacation, [_vacation, sickLeave]));
        Assert.Equal(7m, TakenDays(_vacation, sickLeave));
    }

    [Fact]
    public void Calculate_RevokedCertificateStartingBeforeVacation_CountsFromFirstSickDay()
    {
        // Sick from Wednesday, 3 June: Wednesday, Friday and Monday (the first vacation day) are the three
        // days without certificate (Thursday is Corpus Christi); Tuesday counts as vacation again.
        var sickLeave = ReportSickLeave(new DateOnly(2026, 6, 3), new DateOnly(2026, 6, 9), hasMedicalCertificate: true);

        sickLeave.RevokeMedicalCertificate(_hr, "Kein gültiges Attest", TestRequests.Now);

        Assert.Equal(1m, SickLeaveDuringVacation.ReturnedDays(_vacation, [_vacation, sickLeave]));
        Assert.Equal(9m, TakenDays(_vacation, sickLeave));
    }

    [Fact]
    public void Calculate_RevokedCertificateAfterReturnedDaysWerePlanned_LeavesDaysUncovered()
    {
        // Ten days of entitlement only: the vacation takes them all, the sick leave returns seven,
        // and the employee plans seven new days with them.
        List<LeaveEntitlement> entitlements = [LeaveEntitlement.Create(_employee.Id, 2026, 10m)];
        var vacation = TestRequests.Submit(_employee, VacationStart, VacationEnd, entitlements: entitlements);
        vacation.Approve(_manager, _employee, TestRequests.Now);
        var sickLeave = ReportSickLeave(
            new DateOnly(2026, 6, 8), new DateOnly(2026, 6, 16), hasMedicalCertificate: true, vacation: vacation);
        var laterVacation = TestRequests.Submit(
            _employee,
            new DateOnly(2026, 9, 7),
            new DateOnly(2026, 9, 15),
            existingRequests: [vacation, sickLeave],
            entitlements: entitlements);

        sickLeave.RevokeMedicalCertificate(_hr, "Attest gefälscht", TestRequests.Now);

        var balance = LeaveBalanceCalculator.Calculate(entitlements, [vacation, sickLeave, laterVacation]);
        Assert.Equal(4m, balance.UncoveredDays);
        Assert.False(balance.IsCovered);
    }

    [Fact]
    public void Calculate_SickLeaveOutsideVacation_ReturnsNothing()
    {
        var sickLeave = ReportSickLeave(new DateOnly(2026, 7, 6), new DateOnly(2026, 7, 7));

        Assert.Equal(0m, SickLeaveDuringVacation.ReturnedDays(_vacation, [_vacation, sickLeave]));
        Assert.Equal(10m, TakenDays(_vacation, sickLeave));
    }

    [Fact]
    public void ReturnedDays_SickLeaveOfOtherEmployee_ReturnsNothing()
    {
        var colleague = TestEmployees.Colleague();
        var sickColleague = TestRequests.Submit(colleague, VacationStart, VacationStart.AddDays(1), LeaveType.SickLeave);

        Assert.Equal(0m, SickLeaveDuringVacation.ReturnedDays(_vacation, [_vacation, sickColleague]));
    }

    private LeaveRequest ReportSickLeave(
        DateOnly start,
        DateOnly end,
        bool hasMedicalCertificate = false,
        LeaveRequest? vacation = null) =>
        TestRequests.Submit(
            _employee,
            start,
            end,
            LeaveType.SickLeave,
            existingRequests: [vacation ?? _vacation],
            hasMedicalCertificate: hasMedicalCertificate);

    private decimal TakenDays(LeaveRequest vacation, LeaveRequest sickLeave) =>
        LeaveBalanceCalculator
            .Calculate(TestRequests.StandardEntitlements(_employee), [vacation, sickLeave])
            .Years.Sum(year => year.ApprovedDays);
}
