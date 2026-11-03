using LeaveManagement.Domain.Employees;
using LeaveManagement.Domain.LeaveRequests;
using LeaveManagement.Domain.Tests.TestData;

namespace LeaveManagement.Domain.Tests.LeaveRequests;

public sealed class MedicalCertificateTests
{
    private static readonly byte[] PdfContent = [0x25, 0x50, 0x44, 0x46, 0x2D];

    private readonly Employee _employee = TestEmployees.ReportOf(TestEmployees.Manager());

    [Fact]
    public void Create_ValidFile_KeepsFileDataInUtc()
    {
        var sickLeave = SickLeave(hasMedicalCertificate: true);

        var certificate = MedicalCertificate.Create(sickLeave, "attest.pdf", "application/pdf", PdfContent, TestRequests.Now);

        Assert.Equal(sickLeave.Id, certificate.LeaveRequestId);
        Assert.Equal("attest.pdf", certificate.FileName);
        Assert.Equal("application/pdf", certificate.ContentType);
        Assert.Equal(PdfContent.Length, certificate.SizeInBytes);
        Assert.Equal(PdfContent, certificate.Content);
        Assert.Equal(TestRequests.Now, certificate.UploadedAt);
        Assert.Equal(TimeSpan.Zero, certificate.UploadedAt.Offset);
    }

    [Theory]
    [InlineData(@"C:\Users\lena\Downloads\attest.pdf")]
    [InlineData("../../etc/attest.pdf")]
    [InlineData("  attest.pdf  ")]
    public void Create_FileNameWithPath_KeepsOnlyTheName(string fileName)
    {
        var certificate = MedicalCertificate.Create(
            SickLeave(hasMedicalCertificate: true), fileName, "application/pdf", PdfContent, TestRequests.Now);

        Assert.Equal("attest.pdf", certificate.FileName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("folder/")]
    public void Create_WithoutFileName_Throws(string fileName)
    {
        Assert.Throws<ArgumentException>(() => MedicalCertificate.Create(
            SickLeave(hasMedicalCertificate: true), fileName, "application/pdf", PdfContent, TestRequests.Now));
    }

    [Theory]
    [InlineData("image/gif")]
    [InlineData("text/html")]
    [InlineData("APPLICATION/PDF")]
    public void Create_OtherContentType_Throws(string contentType)
    {
        Assert.Throws<ArgumentException>(() => MedicalCertificate.Create(
            SickLeave(hasMedicalCertificate: true), "attest.pdf", contentType, PdfContent, TestRequests.Now));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(MedicalCertificate.MaxSizeInBytes + 1)]
    public void Create_EmptyOrTooLargeFile_Throws(int size)
    {
        Assert.Throws<ArgumentException>(() => MedicalCertificate.Create(
            SickLeave(hasMedicalCertificate: true), "attest.pdf", "application/pdf", new byte[size], TestRequests.Now));
    }

    [Fact]
    public void Create_FileOfMaximumSize_IsAccepted()
    {
        var certificate = MedicalCertificate.Create(
            SickLeave(hasMedicalCertificate: true),
            "attest.pdf",
            "application/pdf",
            new byte[MedicalCertificate.MaxSizeInBytes],
            TestRequests.Now);

        Assert.Equal(MedicalCertificate.MaxSizeInBytes, certificate.SizeInBytes);
    }

    [Fact]
    public void Create_SickLeaveReportedWithoutCertificate_Throws()
    {
        Assert.Throws<ArgumentException>(() => MedicalCertificate.Create(
            SickLeave(hasMedicalCertificate: false), "attest.pdf", "application/pdf", PdfContent, TestRequests.Now));
    }

    private LeaveRequest SickLeave(bool hasMedicalCertificate) =>
        TestRequests.Submit(
            _employee,
            new DateOnly(2026, 2, 23),
            new DateOnly(2026, 2, 27),
            LeaveType.SickLeave,
            hasMedicalCertificate: hasMedicalCertificate);
}
