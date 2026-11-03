using LeaveManagement.Domain.Holidays;

namespace LeaveManagement.Api.Holidays;

public sealed record HolidayResponse(DateOnly Date, string Name, HolidayKind Kind);
