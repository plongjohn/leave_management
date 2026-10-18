using LeaveManagement.Domain.Employees;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LeaveManagement.Infrastructure.Persistence.Configurations;

internal sealed class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.HasKey(employee => employee.Id);

        // Ids are generated in the domain (Guid v7), never by the database.
        builder.Property(employee => employee.Id).ValueGeneratedNever();

        builder.Property(employee => employee.FirstName).HasMaxLength(Employee.NameMaxLength);
        builder.Property(employee => employee.LastName).HasMaxLength(Employee.NameMaxLength);
        builder.Property(employee => employee.Email).HasMaxLength(Employee.EmailMaxLength);

        // Emails are stored lower-case by the domain, so a plain unique index is case-insensitive.
        builder.HasIndex(employee => employee.Email).IsUnique();

        builder.HasOne<Employee>()
            .WithMany()
            .HasForeignKey(employee => employee.ManagerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
