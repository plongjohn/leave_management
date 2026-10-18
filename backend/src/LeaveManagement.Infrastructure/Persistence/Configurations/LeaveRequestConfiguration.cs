using LeaveManagement.Domain.Employees;
using LeaveManagement.Domain.LeaveRequests;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LeaveManagement.Infrastructure.Persistence.Configurations;

internal sealed class LeaveRequestConfiguration : IEntityTypeConfiguration<LeaveRequest>
{
    public void Configure(EntityTypeBuilder<LeaveRequest> builder)
    {
        builder.HasKey(request => request.Id);
        builder.Property(request => request.Id).ValueGeneratedNever();

        builder.Property(request => request.Type).HasConversion<string>().HasMaxLength(ColumnLengths.Enum);
        builder.Property(request => request.SpecialLeaveReason).HasConversion<string>().HasMaxLength(ColumnLengths.Enum);
        builder.Property(request => request.Status).HasConversion<string>().HasMaxLength(ColumnLengths.Enum);
        builder.Property(request => request.WorkingDays).HasPrecision(ColumnLengths.DaysPrecision, ColumnLengths.DaysScale);
        builder.Property(request => request.Comment).HasMaxLength(LeaveRequest.CommentMaxLength);

        // Npgsql maps a uint row version to PostgreSQL's xmin system column (no extra column).
        builder.Property(request => request.Version).IsRowVersion();

        builder.Ignore(request => request.Period);
        builder.Ignore(request => request.IsActive);

        builder.HasOne<Employee>()
            .WithMany()
            .HasForeignKey(request => request.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Employee>()
            .WithMany()
            .HasForeignKey(request => request.DecidedById)
            .OnDelete(DeleteBehavior.Restrict);

        // Overlap and balance checks load the requests of one employee around a period.
        builder.HasIndex(request => new { request.EmployeeId, request.StartDate });

        builder.OwnsMany(request => request.Days, days =>
        {
            days.ToTable("leave_request_days");
            days.WithOwner().HasForeignKey("LeaveRequestId");
            days.HasKey("LeaveRequestId", nameof(LeaveRequestDay.Date));
            days.Property(day => day.WorkingDays).HasPrecision(ColumnLengths.DaysPrecision, ColumnLengths.DaysScale);

            // The team calendar queries absences by date.
            days.HasIndex(day => day.Date);
        });
    }
}
