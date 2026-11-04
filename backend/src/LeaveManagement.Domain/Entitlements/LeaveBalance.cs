namespace LeaveManagement.Domain.Entitlements;

/// <summary>The result of <see cref="LeaveBalanceCalculator"/> for one employee.</summary>
public sealed class LeaveBalance
{
    internal LeaveBalance(IReadOnlyList<EntitlementYearBalance> years, decimal uncoveredDays)
    {
        Years = years;
        UncoveredDays = uncoveredDays;
    }

    /// <summary>One entry per leave year with an entitlement, oldest first.</summary>
    public IReadOnlyList<EntitlementYearBalance> Years { get; }

    /// <summary>Days of pending or approved vacation that no entitlement covers.</summary>
    public decimal UncoveredDays { get; }

    public bool IsCovered => UncoveredDays == 0;

    /// <summary>The days that can still be taken in <paramref name="year"/>, including those carried over.</summary>
    public decimal AvailableIn(int year) =>
        Years.Where(balance => LeaveEntitlement.IsUsableIn(balance.Year, year)).Sum(balance => balance.RemainingDays);
}
