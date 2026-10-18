using System.Globalization;
using LeaveManagement.Domain.Common;

namespace LeaveManagement.Domain.Entitlements;

/// <summary>Error codes of the leave entitlement rules. Clients map them to user-facing texts.</summary>
public static class EntitlementErrors
{
    public const string TakenDaysNotCovered = "leave_entitlement.taken_days_not_covered";

    internal static BusinessRuleViolationException TakenDaysNotCoveredViolation(decimal uncoveredDays) =>
        new(
            TakenDaysNotCovered,
            string.Create(
                CultureInfo.InvariantCulture,
                $"{uncoveredDays} working days of approved or pending leave would no longer be covered."));
}
