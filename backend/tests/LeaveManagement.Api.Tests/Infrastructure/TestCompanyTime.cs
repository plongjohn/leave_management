namespace LeaveManagement.Api.Tests.Infrastructure;

/// <summary>
/// A clock the tests can set to any time, also backwards: the API host is shared by all tests, so
/// each test resets it. (FakeTimeProvider only moves forward.)
/// </summary>
public sealed class TestCompanyTime(DateTimeOffset utcNow) : TimeProvider
{
    public DateTimeOffset UtcNow { get; set; } = utcNow;

    public override DateTimeOffset GetUtcNow() => UtcNow;
}
