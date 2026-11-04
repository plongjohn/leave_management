using LeaveManagement.Domain.LeaveRequests;

namespace LeaveManagement.Domain.Entitlements;

/// <summary>The annual leave an employee is entitled to for one leave year (= calendar year).</summary>
public sealed class LeaveEntitlement
{
    /// <summary>
    /// 25 working days correspond to the statutory 30 workdays (§ 2 UrlG) for a five-day week (ADR 0007).
    /// </summary>
    public const decimal StandardDays = 25m;

    /// <summary>Entitlements expire at the end of the second year after the leave year (§ 4 Abs 5 UrlG).</summary>
    public const int YearsUntilExpiry = 2;

    // Days are counted in half-day steps because of company half days such as 24 December.
    private const decimal DayStep = 0.5m;

    private const int MinYear = 2000;
    private const int MaxYear = 9999 - YearsUntilExpiry;

    // Used by EF Core when materializing; parameter names match the property names.
    private LeaveEntitlement(Guid id, Guid employeeId, int year, decimal days)
    {
        Id = id;
        EmployeeId = employeeId;
        Year = year;
        Days = days;
    }

    public Guid Id { get; private set; }

    public Guid EmployeeId { get; private set; }

    public int Year { get; private set; }

    public decimal Days { get; private set; }

    public DateOnly ExpiresOn => ExpiryDateFor(Year);

    public static LeaveEntitlement Create(Guid employeeId, int year, decimal days = StandardDays)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(year, MinYear);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(year, MaxYear);
        EnsureValidDays(days);

        return new LeaveEntitlement(Guid.CreateVersion7(), employeeId, year, days);
    }

    public static DateOnly ExpiryDateFor(int year) => new(year + YearsUntilExpiry, 12, 31);

    /// <summary>Whether days of the given leave year may still be taken on a day in <paramref name="year"/>.</summary>
    public static bool IsUsableIn(int entitlementYear, int year) =>
        entitlementYear <= year && year <= entitlementYear + YearsUntilExpiry;

    /// <summary>
    /// Adjusts the entitlement, e.g. for the sixth week after 25 years of service (§ 2 UrlG) or
    /// extra days from the employment contract. Days that are already taken or reserved must stay covered.
    /// </summary>
    /// <param name="days">The new number of working days.</param>
    /// <param name="entitlementsOfEmployee">All entitlements of the employee.</param>
    /// <param name="requestsOfEmployee">All leave requests of the employee.</param>
    public void ChangeDays(
        decimal days,
        IEnumerable<LeaveEntitlement> entitlementsOfEmployee,
        IEnumerable<LeaveRequest> requestsOfEmployee)
    {
        ArgumentNullException.ThrowIfNull(entitlementsOfEmployee);
        ArgumentNullException.ThrowIfNull(requestsOfEmployee);
        EnsureValidDays(days);

        var adjustedEntitlements = entitlementsOfEmployee
            .Where(entitlement => entitlement.EmployeeId == EmployeeId && entitlement.Id != Id)
            .Select(entitlement => (entitlement.Year, entitlement.Days))
            .Append((Year, days));
        var requests = requestsOfEmployee.Where(request => request.EmployeeId == EmployeeId);

        var balance = LeaveBalanceCalculator.Calculate(adjustedEntitlements, requests);
        if (!balance.IsCovered)
        {
            throw EntitlementErrors.TakenDaysNotCoveredViolation(balance.UncoveredDays);
        }

        Days = days;
    }

    private static void EnsureValidDays(decimal days)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(days);

        if (days % DayStep != 0)
        {
            throw new ArgumentException($"Must be a multiple of {DayStep} days.", nameof(days));
        }
    }
}
