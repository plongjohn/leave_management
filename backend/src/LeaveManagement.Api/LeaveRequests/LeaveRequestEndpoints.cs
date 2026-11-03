using System.Security.Claims;
using LeaveManagement.Api.Auth;
using LeaveManagement.Api.Common;
using LeaveManagement.Domain.Employees;
using LeaveManagement.Domain.LeaveRequests;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace LeaveManagement.Api.LeaveRequests;

internal static class LeaveRequestEndpoints
{
    public static IServiceCollection AddLeaveRequestFeature(this IServiceCollection services)
    {
        services.AddScoped<LeaveRequestQueries>();
        services.AddScoped<LeaveRequestService>();
        services.AddScoped<TeamQueries>();
        return services;
    }

    public static IEndpointRouteBuilder MapLeaveRequestEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // "me" routes act on the signed-in employee only, so no id can point to someone else's data.
        var me = endpoints.MapGroup("/api/me").WithTags("My leave");

        me.MapGet("/leave-requests", GetMyLeaveRequestsAsync)
            .WithName("GetMyLeaveRequests")
            .WithValidation<GetMyLeaveRequestsRequest>();

        me.MapGet("/leave-requests/{id:guid}", GetMyLeaveRequestAsync).WithName("GetMyLeaveRequest");

        me.MapGet("/leave-balance", GetMyLeaveBalanceAsync).WithName("GetMyLeaveBalance");

        me.MapPost("/leave-requests/preview", PreviewLeaveRequestAsync)
            .WithName("PreviewLeaveRequest")
            .WithValidation<LeaveRequestDraftRequest>();

        me.MapPost("/leave-requests", SubmitLeaveRequestAsync)
            .WithName("SubmitLeaveRequest")
            .WithValidation<LeaveRequestDraftRequest>()
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        me.MapPost("/leave-requests/{id:guid}/cancel", CancelLeaveRequestAsync)
            .WithName("CancelLeaveRequest")
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        me.MapPost("/sick-leaves", ReportSickLeaveAsync)
            .WithName("ReportSickLeave")
            .WithValidation<ReportSickLeaveForm>()
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .WithMetadata(new RequestSizeLimitAttribute(MaxSickLeaveRequestBytes))
            // Antiforgery protects cookie-authenticated forms; this API authenticates with a bearer
            // token in the Authorization header, which a foreign site cannot send.
            .DisableAntiforgery();

        // Not under "me": HR opens the certificates of other employees, too.
        endpoints.MapGet("/api/leave-requests/{id:guid}/medical-certificate", GetMedicalCertificateAsync)
            .WithName("GetMedicalCertificate")
            .WithTags("Leave requests");

        return endpoints;
    }

    // The largest certificate plus the form fields and the multipart overhead.
    private const long MaxSickLeaveRequestBytes = MedicalCertificate.MaxSizeInBytes + (1024 * 1024);

    private static async Task<Ok<IReadOnlyList<MyLeaveRequestResponse>>> GetMyLeaveRequestsAsync(
        [AsParameters] GetMyLeaveRequestsRequest request,
        ClaimsPrincipal user,
        LeaveRequestQueries queries,
        CancellationToken cancellationToken)
    {
        return TypedResults.Ok(await queries.GetMineAsync(user.GetEmployeeId(), request.Year, cancellationToken));
    }

    private static async Task<Results<Ok<MyLeaveRequestResponse>, NotFound>> GetMyLeaveRequestAsync(
        Guid id,
        ClaimsPrincipal user,
        LeaveRequestQueries queries,
        CancellationToken cancellationToken)
    {
        var request = await queries.GetMineByIdAsync(user.GetEmployeeId(), id, cancellationToken);
        return request is null ? TypedResults.NotFound() : TypedResults.Ok(request);
    }

    private static async Task<Ok<LeaveBalanceResponse>> GetMyLeaveBalanceAsync(
        ClaimsPrincipal user,
        LeaveRequestQueries queries,
        CancellationToken cancellationToken)
    {
        return TypedResults.Ok(await queries.GetMyBalanceAsync(user.GetEmployeeId(), cancellationToken));
    }

    private static async Task<Ok<LeaveRequestPreviewResponse>> PreviewLeaveRequestAsync(
        LeaveRequestDraftRequest request,
        ClaimsPrincipal user,
        LeaveRequestService service,
        CancellationToken cancellationToken)
    {
        return TypedResults.Ok(await service.PreviewAsync(user.GetEmployeeId(), request, cancellationToken));
    }

    // Violated rules become a 422 in BusinessRuleExceptionHandler.
    private static async Task<Results<Created<MyLeaveRequestResponse>, ValidationProblem>> SubmitLeaveRequestAsync(
        LeaveRequestDraftRequest request,
        ClaimsPrincipal user,
        LeaveRequestService service,
        LeaveRequestQueries queries,
        CancellationToken cancellationToken)
    {
        // Sick leave is reported with its certificate in one multipart request (POST /api/me/sick-leaves).
        if (request.Type == LeaveType.SickLeave)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                [nameof(request.Type)] = ["Sick leave is reported with POST /api/me/sick-leaves."],
            });
        }

        var employeeId = user.GetEmployeeId();
        var id = await service.SubmitAsync(employeeId, request, cancellationToken);
        var created = await queries.GetMineByIdAsync(employeeId, id, cancellationToken);

        return TypedResults.Created($"/api/me/leave-requests/{id}", created);
    }

    private static async Task<Created<MyLeaveRequestResponse>> ReportSickLeaveAsync(
        [AsParameters] ReportSickLeaveForm form,
        ClaimsPrincipal user,
        LeaveRequestService service,
        LeaveRequestQueries queries,
        CancellationToken cancellationToken)
    {
        var employeeId = user.GetEmployeeId();
        var id = await service.ReportSickLeaveAsync(employeeId, form, cancellationToken);
        var created = await queries.GetMineByIdAsync(employeeId, id, cancellationToken);

        return TypedResults.Created($"/api/me/leave-requests/{id}", created);
    }

    private static async Task<Results<FileContentHttpResult, NotFound>> GetMedicalCertificateAsync(
        Guid id,
        ClaimsPrincipal user,
        HttpResponse response,
        LeaveRequestQueries queries,
        CancellationToken cancellationToken)
    {
        var certificate = await queries.GetMedicalCertificateAsync(
            id, user.GetEmployeeId(), user.IsInRole(nameof(EmployeeRole.Hr)), cancellationToken);
        if (certificate is null)
        {
            // Also for certificates the user may not see: their existence is not revealed.
            return TypedResults.NotFound();
        }

        // Health data: never cached, and the browser must not guess another type than the one checked.
        response.Headers.CacheControl = "no-store";
        response.Headers.XContentTypeOptions = "nosniff";
        return TypedResults.File(certificate.Content, certificate.ContentType, certificate.FileName);
    }

    private static async Task<Results<NoContent, NotFound>> CancelLeaveRequestAsync(
        Guid id,
        CancelLeaveRequestRequest request,
        ClaimsPrincipal user,
        LeaveRequestService service,
        CancellationToken cancellationToken)
    {
        var cancelled = await service.CancelAsync(user.GetEmployeeId(), id, request.Version, cancellationToken);
        return cancelled ? TypedResults.NoContent() : TypedResults.NotFound();
    }
}
