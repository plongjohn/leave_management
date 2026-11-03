namespace LeaveManagement.Api.LeaveRequests;

/// <summary>The signed-in employee's vacation balance (ADR 0007).</summary>
/// <param name="Years">The entitlements that have not expired yet, oldest first.</param>
/// <param name="AvailableDays">
/// Days that can still be taken this year, carried-over days included. Negative when planned vacation
/// is no longer covered, e.g. after HR revoked a medical certificate.
/// </param>
public sealed record LeaveBalanceResponse(IReadOnlyList<EntitlementYearResponse> Years, decimal AvailableDays);

/// <param name="ApprovedDays">Taken by approved vacation.</param>
/// <param name="PendingDays">Reserved by pending vacation requests.</param>
public sealed record EntitlementYearResponse(
    int Year,
    decimal EntitledDays,
    decimal ApprovedDays,
    decimal PendingDays,
    decimal RemainingDays,
    DateOnly ExpiresOn);
