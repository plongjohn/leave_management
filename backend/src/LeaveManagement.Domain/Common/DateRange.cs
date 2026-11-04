namespace LeaveManagement.Domain.Common;

/// <summary>An inclusive range of calendar days.</summary>
public readonly record struct DateRange
{
    public DateRange(DateOnly start, DateOnly end)
    {
        if (end < start)
        {
            throw new ArgumentException("The end must not be before the start.", nameof(end));
        }

        Start = start;
        End = end;
    }

    public DateOnly Start { get; }

    public DateOnly End { get; }

    public bool Overlaps(DateRange other) => Start <= other.End && other.Start <= End;

    public bool Contains(DateOnly date) => Start <= date && date <= End;

    public IEnumerable<DateOnly> Days()
    {
        for (var day = Start; day <= End; day = day.AddDays(1))
        {
            yield return day;
        }
    }
}
