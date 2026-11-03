using System.Text.Json;
using LeaveManagement.Api.Common;
using LeaveManagement.Domain.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace LeaveManagement.Api.Tests.Common;

/// <summary>
/// The handler on its own; the endpoint tests of the following features cover it end to end.
/// </summary>
public sealed class BusinessRuleExceptionHandlerTests
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task TryHandle_BusinessRuleViolation_WritesUnprocessableEntityWithCode()
    {
        var httpContext = CreateHttpContext();

        var handled = await CreateHandler(httpContext).TryHandleAsync(
            httpContext,
            new BusinessRuleViolationException("leave_request.overlapping", "Overlaps."),
            CancellationToken);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status422UnprocessableEntity, httpContext.Response.StatusCode);
        var problem = await ReadProblemAsync(httpContext);
        Assert.Equal("leave_request.overlapping", problem.GetProperty("code").GetString());
        Assert.Equal("Overlaps.", problem.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task TryHandle_ConcurrencyConflict_WritesConflictWithCode()
    {
        var httpContext = CreateHttpContext();

        var handled = await CreateHandler(httpContext).TryHandleAsync(
            httpContext,
            new DbUpdateConcurrencyException(),
            CancellationToken);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status409Conflict, httpContext.Response.StatusCode);
        var problem = await ReadProblemAsync(httpContext);
        Assert.Equal(ApiErrors.ConcurrencyConflict, problem.GetProperty("code").GetString());
    }

    [Fact]
    public async Task TryHandle_OtherException_LeavesItToTheDefaultHandler()
    {
        var httpContext = CreateHttpContext();

        var handled = await CreateHandler(httpContext).TryHandleAsync(
            httpContext,
            new InvalidOperationException(),
            CancellationToken);

        Assert.False(handled);
        Assert.Equal(0, httpContext.Response.Body.Length);
    }

    private static DefaultHttpContext CreateHttpContext()
    {
        var services = new ServiceCollection().AddLogging().AddProblemDetails().BuildServiceProvider();
        return new DefaultHttpContext
        {
            RequestServices = services,
            Response = { Body = new MemoryStream() },
        };
    }

    private static BusinessRuleExceptionHandler CreateHandler(HttpContext httpContext) =>
        new(
            httpContext.RequestServices.GetRequiredService<IProblemDetailsService>(),
            NullLogger<BusinessRuleExceptionHandler>.Instance);

    private static async Task<JsonElement> ReadProblemAsync(HttpContext httpContext)
    {
        httpContext.Response.Body.Position = 0;
        using var document = await JsonDocument.ParseAsync(httpContext.Response.Body, cancellationToken: CancellationToken);
        return document.RootElement.Clone();
    }
}
