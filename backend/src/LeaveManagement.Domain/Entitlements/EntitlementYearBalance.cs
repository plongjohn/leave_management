namespace LeaveManagement.Domain.Entitlements;

/// <summary>How the entitlement of one leave year is used.</summary>
/// <param name="Year">The leave year the entitlement belongs to.</param>
/// <param name="EntitledDays">The entitlement of that year.</param>
/// <param name="ApprovedDays">Days of approved vacation taken from this entitlement.</param>
/// <param name="PendingDays">Days reserved by pending vacation requests.</param>
public sealed record EntitlementYearBalance(int Year, decimal EntitledDays, decimal ApprovedDays, decimal PendingDays)
{
    public decimal RemainingDays => EntitledDays - ApprovedDays - PendingDays;

    public DateOnly ExpiresOn => LeaveEntitlement.ExpiryDateFor(Year);
}
