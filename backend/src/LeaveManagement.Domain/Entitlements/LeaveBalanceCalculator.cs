using LeaveManagement.Domain.LeaveRequests;

namespace LeaveManagement.Domain.Entitlements;

/// <summary>
/// Allocates vacation days to the entitlements of the leave years (ADR 0007): every day of pending or
/// approved vacation is taken from the oldest entitlement that has not expired on that day. Remaining
/// days are therefore carried over implicitly and never stored.
/// </summary>
public static class LeaveBalanceCalculator
{
    /// <param name="entitlements">All entitlements of one employee.</param>
    /// <param name="requests">All leave requests of the same employee; only pending and approved vacation counts.</param>
    public static LeaveBalance Calculate(IEnumerable<LeaveEntitlement> entitlements, IEnumerable<LeaveRequest> requests)
    {
        ArgumentNullException.ThrowIfNull(entitlements);

        return Calculate(entitlements.Select(entitlement => (entitlement.Year, entitlement.Days)), requests);
    }

    internal static LeaveBalance Calculate(
        IEnumerable<(int Year, decimal Days)> entitlements,
        IEnumerable<LeaveRequest> requests)
    {
        ArgumentNullException.ThrowIfNull(requests);

        var pots = entitlements
            .OrderBy(entitlement => entitlement.Year)
            .Select(entitlement => new Pot(entitlement.Year, entitlement.Days))
            .ToList();

        if (pots.Select(pot => pot.Year).Distinct().Count() != pots.Count)
        {
            throw new ArgumentException("There must be at most one entitlement per year.", nameof(entitlements));
        }

        var vacationDays = requests
            .Where(request => request.Type == LeaveType.Vacation && request.IsActive)
            .SelectMany(request => request.Days.Select(day =>
                (day.Date, day.WorkingDays, IsPending: request.Status == LeaveRequestStatus.Pending)))
            .OrderBy(day => day.Date);

        var uncoveredDays = 0m;
        foreach (var day in vacationDays)
        {
            var openDays = day.WorkingDays;
            foreach (var pot in pots.Where(pot => LeaveEntitlement.IsUsableIn(pot.Year, day.Date.Year)))
            {
                openDays -= pot.Take(openDays, day.IsPending);
                if (openDays == 0)
                {
                    break;
                }
            }

            uncoveredDays += openDays;
        }

        return new LeaveBalance([.. pots.Select(pot => pot.ToBalance())], uncoveredDays);
    }

    /// <summary>The entitlement of one leave year while days are allocated to it.</summary>
    private sealed class Pot(int year, decimal entitledDays)
    {
        private decimal _approvedDays;
        private decimal _pendingDays;

        public int Year { get; } = year;

        public decimal Take(decimal days, bool isPending)
        {
            var taken = Math.Min(days, entitledDays - _approvedDays - _pendingDays);
            if (isPending)
            {
                _pendingDays += taken;
            }
            else
            {
                _approvedDays += taken;
            }

            return taken;
        }

        public EntitlementYearBalance ToBalance() => new(Year, entitledDays, _approvedDays, _pendingDays);
    }
}
