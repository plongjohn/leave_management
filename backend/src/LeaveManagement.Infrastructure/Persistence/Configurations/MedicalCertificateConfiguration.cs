using LeaveManagement.Domain.LeaveRequests;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LeaveManagement.Infrastructure.Persistence.Configurations;

/// <summary>
/// Certificates are stored in PostgreSQL (bytea) rather than in a file store: they are small, rare and
/// must stay consistent with their request, and backups then cover them too (ADR 0008).
/// </summary>
internal sealed class MedicalCertificateConfiguration : IEntityTypeConfiguration<MedicalCertificate>
{
    public void Configure(EntityTypeBuilder<MedicalCertificate> builder)
    {
        builder.HasKey(certificate => certificate.Id);
        builder.Property(certificate => certificate.Id).ValueGeneratedNever();

        builder.Property(certificate => certificate.FileName).HasMaxLength(MedicalCertificate.FileNameMaxLength);
        builder.Property(certificate => certificate.ContentType).HasMaxLength(MedicalCertificate.ContentTypeMaxLength);

        // One certificate per sick leave; it is deleted together with its request.
        builder.HasOne<LeaveRequest>()
            .WithOne()
            .HasForeignKey<MedicalCertificate>(certificate => certificate.LeaveRequestId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
