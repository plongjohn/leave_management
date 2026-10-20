namespace LeaveManagement.Infrastructure.Persistence.Seeding;

/// <param name="UserPassword">
/// Password of all demo logins, from the environment variable <c>Seed__DevelopmentPassword</c> –
/// never committed, not even for development.
/// </param>
public sealed record DevelopmentSeedOptions(string? UserPassword);
