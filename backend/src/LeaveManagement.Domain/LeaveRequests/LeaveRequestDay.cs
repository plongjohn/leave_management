namespace LeaveManagement.Domain.LeaveRequests;

/// <summary>One working day (or half day) taken by a leave request.</summary>
public sealed class LeaveRequestDay
{
    internal LeaveRequestDay(DateOnly date, decimal workingDays)
    {
        Date = date;
        WorkingDays = workingDays;
    }

    public DateOnly Date { get; private set; }

    public decimal WorkingDays { get; private set; }
}
