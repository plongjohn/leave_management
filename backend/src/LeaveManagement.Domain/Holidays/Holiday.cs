namespace LeaveManagement.Domain.Holidays;

/// <summary>A day that is not, or only half, a working day. One entry per date.</summary>
public sealed class Holiday
{
    public const int NameMaxLength = 100;

    // Used by EF Core when materializing; parameter names match the property names.
    private Holiday(DateOnly date, string name, HolidayKind kind)
    {
        Date = date;
        Name = name;
        Kind = kind;
    }

    public DateOnly Date { get; private set; }

    public string Name { get; private set; }

    public HolidayKind Kind { get; private set; }

    public static Holiday Create(DateOnly date, string name, HolidayKind kind)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var trimmed = name.Trim();
        if (trimmed.Length > NameMaxLength)
        {
            throw new ArgumentException($"Must not be longer than {NameMaxLength} characters.", nameof(name));
        }

        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind));
        }

        return new Holiday(date, trimmed, kind);
    }
}
