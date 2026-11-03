using LeaveManagement.Domain.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LeaveManagement.Api.Common;

/// <summary>
/// Turns the expected failures of a request into ProblemDetails with a stable <c>code</c>:
/// a violated business rule becomes 422, a concurrent change of the same data 409.
/// Endpoints therefore do not catch these exceptions themselves. Everything else stays a 500.
/// </summary>
internal sealed partial class BusinessRuleExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<BusinessRuleExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var problem = ToProblem(exception);
        if (problem is null)
        {
            return false;
        }

        LogHandled(logger, problem.Status, problem.Extensions["code"]);

        httpContext.Response.StatusCode = problem.Status!.Value;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception,
        });
    }

    private static ProblemDetails? ToProblem(Exception exception) => exception switch
    {
        BusinessRuleViolationException violation => Problem(
            StatusCodes.Status422UnprocessableEntity,
            "A business rule prevents this change.",
            violation.Message,
            violation.Code),
        DbUpdateConcurrencyException => Problem(
            StatusCodes.Status409Conflict,
            "The data was changed in the meantime.",
            "Someone else changed the data. Reload it and try again.",
            ApiErrors.ConcurrencyConflict),
        _ => null,
    };

    private static ProblemDetails Problem(int status, string title, string detail, string code) => new()
    {
        Status = status,
        Title = title,
        Detail = detail,
        Extensions = { ["code"] = code },
    };

    // Expected outcomes, not errors of the application; the code is enough to follow them in the logs.
    [LoggerMessage(Level = LogLevel.Information, Message = "Request rejected with {StatusCode}: {Code}")]
    private static partial void LogHandled(ILogger logger, int? statusCode, object? code);
}
