using LeaveManagement.Domain.Holidays;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LeaveManagement.Infrastructure.Persistence.Configurations;

internal sealed class HolidayConfiguration : IEntityTypeConfiguration<Holiday>
{
    // Reference data shipped with the migrations (ADR 0007). Extending the range needs a new migration.
    private const int FirstSeededYear = 2025;
    private const int LastSeededYear = 2035;

    public void Configure(EntityTypeBuilder<Holiday> builder)
    {
        // There is at most one entry per day, so the date is the natural key.
        builder.HasKey(holiday => holiday.Date);

        builder.Property(holiday => holiday.Name).HasMaxLength(Holiday.NameMaxLength);
        builder.Property(holiday => holiday.Kind).HasConversion<string>().HasMaxLength(ColumnLengths.Enum);

        builder.HasData(SeedData());
    }

    private static IEnumerable<Holiday> SeedData()
    {
        return Enumerable.Range(FirstSeededYear, LastSeededYear - FirstSeededYear + 1)
            .SelectMany(year => AustrianPublicHolidays.For(year).Concat(CompanyHalfDays(year)));
    }

    // Not statutory, but common in Austrian companies; HR can change them (phase 6).
    private static IEnumerable<Holiday> CompanyHalfDays(int year) =>
    [
        Holiday.Create(new DateOnly(year, 12, 24), "Heiliger Abend", HolidayKind.CompanyHalfDay),
        Holiday.Create(new DateOnly(year, 12, 31), "Silvester", HolidayKind.CompanyHalfDay),
    ];
}
