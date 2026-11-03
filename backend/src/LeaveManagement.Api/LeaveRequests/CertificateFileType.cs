namespace LeaveManagement.Api.LeaveRequests;

/// <summary>
/// Recognises the type of an uploaded certificate by its first bytes ("magic numbers"). The file name
/// and the content type sent by the client can be anything, so they are not trusted.
/// </summary>
internal static class CertificateFileType
{
    /// <summary>Enough bytes to recognise every supported type.</summary>
    public const int HeaderLength = 8;

    private static readonly (byte[] Signature, string ContentType)[] KnownTypes =
    [
        ("%PDF-"u8.ToArray(), "application/pdf"),
        ([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A], "image/png"),
        ([0xFF, 0xD8, 0xFF], "image/jpeg"),
    ];

    /// <returns>The content type, or <c>null</c> if the file is not a PDF, PNG or JPEG.</returns>
    public static string? Detect(ReadOnlySpan<byte> header)
    {
        foreach (var (signature, contentType) in KnownTypes)
        {
            if (header.StartsWith(signature))
            {
                return contentType;
            }
        }

        return null;
    }

    public static async Task<string?> DetectAsync(IFormFile file, CancellationToken cancellationToken)
    {
        var header = new byte[HeaderLength];
        await using var stream = file.OpenReadStream();
        var read = await stream.ReadAtLeastAsync(header, HeaderLength, throwOnEndOfStream: false, cancellationToken);
        return Detect(header.AsSpan(0, read));
    }
}
