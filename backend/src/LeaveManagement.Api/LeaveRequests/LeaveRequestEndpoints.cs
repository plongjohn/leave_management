using System.Security.Claims;
using LeaveManagement.Api.Auth;
using LeaveManagement.Api.Common;
using LeaveManagement.Domain.LeaveRequests;
using Microsoft.AspNetCore.Http.HttpResults;

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

        return endpoints;
    }

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
