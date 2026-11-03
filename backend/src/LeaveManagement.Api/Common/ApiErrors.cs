namespace LeaveManagement.Api.Common;

/// <summary>
/// Error codes that are not owned by a domain area, returned as <c>code</c> in ProblemDetails.
/// Clients map them to user-facing texts.
/// </summary>
public static class ApiErrors
{
    public const string ConcurrencyConflict = "concurrency_conflict";
}
