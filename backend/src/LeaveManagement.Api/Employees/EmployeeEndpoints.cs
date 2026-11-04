using Microsoft.AspNetCore.Http.HttpResults;

namespace LeaveManagement.Api.Employees;

internal static class EmployeeEndpoints
{
    public static IServiceCollection AddEmployeeFeature(this IServiceCollection services)
    {
        services.AddScoped<EmployeeQueries>();
        return services;
    }

    public static IEndpointRouteBuilder MapEmployeeEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/employees").WithTags("Employees");

        group.MapGet("/", GetEmployeesAsync).WithName("GetEmployees");

        return endpoints;
    }

    private static async Task<Ok<IReadOnlyList<EmployeeListItemResponse>>> GetEmployeesAsync(
        EmployeeQueries queries,
        CancellationToken cancellationToken)
    {
        return TypedResults.Ok(await queries.GetListAsync(cancellationToken));
    }
}
