using FluentValidation;
using LeaveManagement.Domain.Common;
using LeaveManagement.Domain.LeaveRequests;

namespace LeaveManagement.Api.LeaveRequests;

/// <summary>What the employee enters in the request dialog; used for the preview and the submission.</summary>
/// <param name="HasMedicalCertificate">Sick leave only: a certificate will be uploaded with the report.</param>
public sealed record LeaveRequestDraftRequest(
    DateOnly StartDate,
    DateOnly EndDate,
    LeaveType Type,
    string? Comment = null,
    SpecialLeaveReason? SpecialLeaveReason = null,
    bool HasMedicalCertificate = false)
{
    public LeaveRequestDraft ToDraft() =>
        new(new DateRange(StartDate, EndDate), Type, Comment, SpecialLeaveReason, HasMedicalCertificate);
}

/// <summary>
/// Checks the shape of the input only. Rules such as "special leave needs a reason" stay in the
/// domain, which answers with a code the client can show (ADR 0007).
/// </summary>
internal sealed class LeaveRequestDraftRequestValidator : AbstractValidator<LeaveRequestDraftRequest>
{
    // Limits the work for absurd input; no real absence lasts longer than a year.
    private const int MaxCalendarDays = 366;

    public LeaveRequestDraftRequestValidator()
    {
        RuleFor(request => request.EndDate)
            .GreaterThanOrEqualTo(request => request.StartDate)
            .WithMessage("The end must not be before the start.");
        RuleFor(request => request.EndDate)
            .Must((request, end) => end.DayNumber - request.StartDate.DayNumber < MaxCalendarDays)
            .When(request => request.EndDate >= request.StartDate)
            .WithMessage($"A request must not be longer than {MaxCalendarDays} days.");
        RuleFor(request => request.Type).IsInEnum();
        RuleFor(request => request.SpecialLeaveReason).IsInEnum();
        RuleFor(request => request.Comment).MaximumLength(LeaveRequest.CommentMaxLength);
        RuleFor(request => request.HasMedicalCertificate)
            .Equal(false)
            .When(request => request.Type != LeaveType.SickLeave)
            .WithMessage("Only sick leave comes with a medical certificate.");
    }
}
