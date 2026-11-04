using LeaveManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LeaveManagement.Api.Holidays;

internal sealed class HolidayQueries(LeaveManagementDbContext dbContext)
{
    public async Task<IReadOnlyList<HolidayResponse>> GetForYearAsync(int year, CancellationToken cancellationToken)
    {
        var firstDay = new DateOnly(year, 1, 1);
        var lastDay = new DateOnly(year, 12, 31);

        return await dbContext.Holidays
            .AsNoTracking()
            .Where(holiday => holiday.Date >= firstDay && holiday.Date <= lastDay)
            .OrderBy(holiday => holiday.Date)
            .Select(holiday => new HolidayResponse(holiday.Date, holiday.Name, holiday.Kind))
            .ToListAsync(cancellationToken);
    }
}
