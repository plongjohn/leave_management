using FluentValidation;

namespace LeaveManagement.Api.Holidays;

/// <summary>Query string of <c>GET /api/holidays</c>.</summary>
public sealed record GetHolidaysRequest(int Year);

internal sealed class GetHolidaysRequestValidator : AbstractValidator<GetHolidaysRequest>
{
    public GetHolidaysRequestValidator()
    {
        // Any year a date can have; years without holidays simply return an empty list.
        RuleFor(request => request.Year).InclusiveBetween(DateOnly.MinValue.Year, DateOnly.MaxValue.Year);
    }
}
