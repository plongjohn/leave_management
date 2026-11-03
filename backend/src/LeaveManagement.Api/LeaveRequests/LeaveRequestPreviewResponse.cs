using LeaveManagement.Api.Holidays;

namespace LeaveManagement.Api.LeaveRequests;

/// <summary>
/// The result of checking a request without saving it, so the dialog can show the effect while the
/// employee types. The rules run in the backend only (coding guidelines).
/// </summary>
/// <param name="WorkingDays">The working days in the period, holidays and half days considered.</param>
/// <param name="HolidaysInPeriod">Holidays and company days in the period, to explain the count.</param>
/// <param name="AvailableDaysAfter">Vacation only: the days still available in the start year afterwards.</param>
/// <param name="ReturnedVacationDays">Sick leave only: approved vacation days that would come back.</param>
/// <param name="ErrorCode">The rule the request would violate, or <c>null</c> if it can be submitted.</param>
/// <param name="NeedsApproval">False for sick leave, which is approved when reported.</param>
/// <param name="ApproverName">The manager who decides; <c>null</c> when HR decides or no approval is needed.</param>
/// <param name="AbsentColleagues">Team members with a pending or approved absence in the period.</param>
public sealed record LeaveRequestPreviewResponse(
    decimal WorkingDays,
    IReadOnlyList<HolidayResponse> HolidaysInPeriod,
    decimal? AvailableDaysAfter,
    decimal ReturnedVacationDays,
    string? ErrorCode,
    bool NeedsApproval,
    string? ApproverName,
    IReadOnlyList<AbsentColleagueResponse> AbsentColleagues);

/// <summary>Only who is away: colleagues do not see the type of someone else's absence (ADR 0007).</summary>
public sealed record AbsentColleagueResponse(Guid EmployeeId, string Name);
