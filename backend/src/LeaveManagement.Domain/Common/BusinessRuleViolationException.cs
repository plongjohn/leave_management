namespace LeaveManagement.Domain.Common;

/// <summary>
/// A business rule prevents the requested change. <see cref="Code"/> is stable and meant for
/// clients (the API returns it in a ProblemDetails response), the message is for developers and logs.
/// </summary>
public sealed class BusinessRuleViolationException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}
