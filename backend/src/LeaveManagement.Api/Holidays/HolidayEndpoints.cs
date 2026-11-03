using LeaveManagement.Api.Common;
using Microsoft.AspNetCore.Http.HttpResults;

namespace LeaveManagement.Api.Holidays;

internal static class HolidayEndpoints
{
    public static IServiceCollection AddHolidayFeature(this IServiceCollection services)
    {
        services.AddScoped<HolidayQueries>();
        return services;
    }

    public static IEndpointRouteBuilder MapHolidayEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // Readable for every signed-in user: the date picker and the team calendar mark holidays.
        var group = endpoints.MapGroup("/api/holidays").WithTags("Holidays");

        group.MapGet("/", GetHolidaysAsync)
            .WithName("GetHolidays")
            .WithValidation<GetHolidaysRequest>();

        return endpoints;
    }

    private static async Task<Ok<IReadOnlyList<HolidayResponse>>> GetHolidaysAsync(
        [AsParameters] GetHolidaysRequest request,
        HolidayQueries queries,
        CancellationToken cancellationToken)
    {
        return TypedResults.Ok(await queries.GetForYearAsync(request.Year, cancellationToken));
    }
}
