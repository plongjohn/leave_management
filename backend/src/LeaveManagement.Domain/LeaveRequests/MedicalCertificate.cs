namespace LeaveManagement.Domain.LeaveRequests;

/// <summary>
/// The file of a medical certificate that came with a sick leave report. Health data (GDPR Art. 9):
/// only the employee and HR may read it. Kept apart from <see cref="LeaveRequest"/> so that loading a
/// request never loads the file.
/// </summary>
public sealed class MedicalCertificate
{
    // 255 characters is the usual file name limit of common file systems.
    public const int FileNameMaxLength = 255;

    public const int ContentTypeMaxLength = 100;

    /// <summary>5 MB: enough for a scanned or photographed certificate (decision 2026-11-03).</summary>
    public const int MaxSizeInBytes = 5 * 1024 * 1024;

    /// <summary>The file types accepted for certificates: PDF and the usual photo formats.</summary>
    public static readonly IReadOnlySet<string> AllowedContentTypes =
        new HashSet<string>(["application/pdf", "image/jpeg", "image/png"], StringComparer.Ordinal);

    // Used by EF Core when materializing; parameter names match the property names.
    private MedicalCertificate(
        Guid id,
        Guid leaveRequestId,
        string fileName,
        string contentType,
        int sizeInBytes,
        byte[] content,
        DateTimeOffset uploadedAt)
    {
        Id = id;
        LeaveRequestId = leaveRequestId;
        FileName = fileName;
        ContentType = contentType;
        SizeInBytes = sizeInBytes;
        Content = content;
        UploadedAt = uploadedAt;
    }

    public Guid Id { get; private set; }

    public Guid LeaveRequestId { get; private set; }

    /// <summary>The name of the uploaded file, without any path.</summary>
    public string FileName { get; private set; }

    public string ContentType { get; private set; }

    /// <summary>Stored separately, so lists can show the size without loading the file.</summary>
    public int SizeInBytes { get; private set; }

    public byte[] Content { get; private set; }

    /// <summary>Stored in UTC, like all timestamps.</summary>
    public DateTimeOffset UploadedAt { get; private set; }

    /// <param name="sickLeave">The sick leave the certificate belongs to; reported with a certificate.</param>
    /// <param name="fileName">The client's file name; a path in it is dropped.</param>
    public static MedicalCertificate Create(
        LeaveRequest sickLeave,
        string fileName,
        string contentType,
        byte[] content,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(sickLeave);
        ArgumentNullException.ThrowIfNull(content);

        if (!sickLeave.HasMedicalCertificate)
        {
            throw new ArgumentException("The sick leave was reported without a medical certificate.", nameof(sickLeave));
        }

        if (!AllowedContentTypes.Contains(contentType))
        {
            throw new ArgumentException("Only PDF, JPEG and PNG files are accepted.", nameof(contentType));
        }

        if (content.Length == 0 || content.Length > MaxSizeInBytes)
        {
            throw new ArgumentException($"Must contain between 1 and {MaxSizeInBytes} bytes.", nameof(content));
        }

        return new MedicalCertificate(
            Guid.CreateVersion7(),
            sickLeave.Id,
            NormalizeFileName(fileName),
            contentType,
            content.Length,
            content,
            now.ToUniversalTime());
    }

    // Browsers send only the name, but other clients may send a full path; it must never be kept.
    private static string NormalizeFileName(string fileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        var name = fileName.Replace('\\', '/').Split('/')[^1].Trim();
        if (name.Length == 0 || name.Length > FileNameMaxLength)
        {
            throw new ArgumentException($"Must be a file name of 1 to {FileNameMaxLength} characters.", nameof(fileName));
        }

        return name;
    }
}
