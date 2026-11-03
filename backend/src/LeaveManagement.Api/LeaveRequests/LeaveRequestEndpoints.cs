using System.Security.Claims;
using LeaveManagement.Api.Auth;
using LeaveManagement.Api.Common;
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

        me.MapGet("/leave-balance", GetMyLeaveBalanceAsync).WithName("GetMyLeaveBalance");

        me.MapPost("/leave-requests/preview", PreviewLeaveRequestAsync)
            .WithName("PreviewLeaveRequest")
            .WithValidation<LeaveRequestDraftRequest>();

        return endpoints;
    }

    private static async Task<Ok<LeaveRequestPreviewResponse>> PreviewLeaveRequestAsync(
        LeaveRequestDraftRequest request,
        ClaimsPrincipal user,
        LeaveRequestService service,
        CancellationToken cancellationToken)
    {
        return TypedResults.Ok(await service.PreviewAsync(user.GetEmployeeId(), request, cancellationToken));
    }

    private static async Task<Ok<IReadOnlyList<MyLeaveRequestResponse>>> GetMyLeaveRequestsAsync(
        [AsParameters] GetMyLeaveRequestsRequest request,
        ClaimsPrincipal user,
        LeaveRequestQueries queries,
        CancellationToken cancellationToken)
    {
        return TypedResults.Ok(await queries.GetMineAsync(user.GetEmployeeId(), request.Year, cancellationToken));
    }

    private static async Task<Ok<LeaveBalanceResponse>> GetMyLeaveBalanceAsync(
        ClaimsPrincipal user,
        LeaveRequestQueries queries,
        CancellationToken cancellationToken)
    {
        return TypedResults.Ok(await queries.GetMyBalanceAsync(user.GetEmployeeId(), cancellationToken));
    }
}
