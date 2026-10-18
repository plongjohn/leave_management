namespace LeaveManagement.Domain.Employees;

public sealed class Employee
{
    public const int NameMaxLength = 100;

    // 254 characters is the maximum length of a usable email address (RFC 5321 path limit).
    public const int EmailMaxLength = 254;

    // Used by EF Core when materializing; parameter names match the property names.
    private Employee(Guid id, string firstName, string lastName, string email, DateOnly entryDate, Guid? managerId)
    {
        Id = id;
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        EntryDate = entryDate;
        ManagerId = managerId;
    }

    public Guid Id { get; private set; }

    public string FirstName { get; private set; }

    public string LastName { get; private set; }

    public string Email { get; private set; }

    public DateOnly EntryDate { get; private set; }

    public Guid? ManagerId { get; private set; }

    public static Employee Create(string firstName, string lastName, string email, DateOnly entryDate, Guid? managerId = null)
    {
        return new Employee(
            Guid.CreateVersion7(),
            RequireText(firstName, NameMaxLength, nameof(firstName)),
            RequireText(lastName, NameMaxLength, nameof(lastName)),
            RequireEmail(email),
            entryDate,
            managerId);
    }

    private static string RequireText(string value, int maxLength, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
        {
            throw new ArgumentException($"Must not be longer than {maxLength} characters.", parameterName);
        }

        return trimmed;
    }

    // Only a basic plausibility check; the API validates the format before the domain is called.
    // Stored lower-case so that "Anna@firma.at" and "anna@firma.at" count as the same address.
    private static string RequireEmail(string email)
    {
        var trimmed = RequireText(email, EmailMaxLength, nameof(email)).ToLowerInvariant();
        if (!trimmed.Contains('@', StringComparison.Ordinal))
        {
            throw new ArgumentException("Must be an email address.", nameof(email));
        }

        return trimmed;
    }
}
