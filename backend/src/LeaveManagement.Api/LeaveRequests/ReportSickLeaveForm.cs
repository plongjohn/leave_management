using FluentValidation;
using LeaveManagement.Domain.LeaveRequests;
using Microsoft.AspNetCore.Mvc;

namespace LeaveManagement.Api.LeaveRequests;

/// <summary>
/// The multipart form of <c>POST /api/me/sick-leaves</c>: the report and its certificate arrive
/// together, so a sick leave that needs a certificate is never saved without one.
/// </summary>
/// <param name="Certificate">Optional; required for more than three working days during vacation.</param>
public sealed record ReportSickLeaveForm(
    [property: FromForm] DateOnly StartDate,
    [property: FromForm] DateOnly EndDate,
    [property: FromForm] string? Comment,
    IFormFile? Certificate)
{
    public LeaveRequestDraftRequest ToDraftRequest() =>
        new(StartDate, EndDate, LeaveType.SickLeave, Comment, HasMedicalCertificate: Certificate is not null);
}

internal sealed class ReportSickLeaveFormValidator : AbstractValidator<ReportSickLeaveForm>
{
    public ReportSickLeaveFormValidator()
    {
        // The same shape rules as LeaveRequestDraftRequestValidator, on the form's own field names.
        RuleFor(form => form.EndDate)
            .GreaterThanOrEqualTo(form => form.StartDate)
            .WithMessage("The end must not be before the start.");
        RuleFor(form => form.EndDate)
            .Must((form, end) => end.DayNumber - form.StartDate.DayNumber < LeaveRequestDraftRequestValidator.MaxCalendarDays)
            .When(form => form.EndDate >= form.StartDate)
            .WithMessage($"A request must not be longer than {LeaveRequestDraftRequestValidator.MaxCalendarDays} days.");
        RuleFor(form => form.Comment).MaximumLength(LeaveRequest.CommentMaxLength);

        RuleFor(form => form.Certificate!.Length)
            .InclusiveBetween(1, MedicalCertificate.MaxSizeInBytes)
            .OverridePropertyName(nameof(ReportSickLeaveForm.Certificate))
            .WithMessage($"The certificate must not be empty or larger than {MedicalCertificate.MaxSizeInBytes} bytes.")
            .When(form => form.Certificate is not null);

        RuleFor(form => form.Certificate!)
            .MustAsync(async (file, cancellationToken) => await CertificateFileType.DetectAsync(file, cancellationToken) is not null)
            .WithMessage("The certificate must be a PDF, JPEG or PNG file.")
            .When(form => form.Certificate is { Length: > 0 });
    }
}
