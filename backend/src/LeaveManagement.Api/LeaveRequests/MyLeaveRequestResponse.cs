using LeaveManagement.Domain.LeaveRequests;

namespace LeaveManagement.Api.LeaveRequests;

/// <summary>One of the signed-in employee's own requests.</summary>
/// <param name="ReturnedDays">Vacation only: working days that count as sickness and are not taken.</param>
/// <param name="ApproverName">Pending only: the manager who decides; <c>null</c> means HR decides.</param>
/// <param name="CanBeCancelled">Whether the employee may cancel the request today.</param>
/// <param name="Version">Concurrency token; send it back when cancelling.</param>
public sealed record MyLeaveRequestResponse(
    Guid Id,
    DateOnly StartDate,
    DateOnly EndDate,
    LeaveType Type,
    SpecialLeaveReason? SpecialLeaveReason,
    decimal WorkingDays,
    decimal ReturnedDays,
    LeaveRequestStatus Status,
    string? Comment,
    bool HasMedicalCertificate,
    bool IsCertificateRevoked,
    DateTimeOffset CreatedAt,
    string? ApproverName,
    string? DecidedByName,
    DateTimeOffset? DecidedAt,
    bool CanBeCancelled,
    uint Version);
