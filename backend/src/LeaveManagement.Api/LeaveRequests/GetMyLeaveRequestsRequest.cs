using FluentValidation;

namespace LeaveManagement.Api.LeaveRequests;

/// <summary>Query string of <c>GET /api/me/leave-requests</c>.</summary>
public sealed record GetMyLeaveRequestsRequest(int Year);

internal sealed class GetMyLeaveRequestsRequestValidator : AbstractValidator<GetMyLeaveRequestsRequest>
{
    public GetMyLeaveRequestsRequestValidator()
    {
        RuleFor(request => request.Year).InclusiveBetween(DateOnly.MinValue.Year, DateOnly.MaxValue.Year);
    }
}
