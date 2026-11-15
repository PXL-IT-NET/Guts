using Guts.Domain.DeletionJobAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Guts.Infrastructure.EntityConfigurations;

internal class DeletionJobConfiguration : IEntityTypeConfiguration<DeletionJob>
{
    public void Configure(EntityTypeBuilder<DeletionJob> builder)
    {
        builder.ToTable("DeletionJobs");

        builder.HasKey(j => j.Id);
        builder.Property(j => j.Id).ValueGeneratedNever();

        builder.Property(j => j.TargetType).IsRequired();
        builder.Property(j => j.TargetId).IsRequired();
        builder.Property(j => j.TargetLabel)
            .IsRequired()
            .HasMaxLength(DeletionJob.MaxTargetLabelLength);
        builder.Property(j => j.Status).IsRequired();
        builder.Property(j => j.TotalRows).IsRequired();
        builder.Property(j => j.DeletedRows).IsRequired();
        builder.Property(j => j.CurrentPhase)
            .IsRequired()
            .HasMaxLength(DeletionJob.MaxCurrentPhaseLength);
        builder.Property(j => j.ErrorMessage)
            .HasMaxLength(DeletionJob.MaxErrorMessageLength);
        builder.Property(j => j.CreatedAtUtc).IsRequired();

        builder.HasIndex(j => new { j.Status, j.TargetType });
    }
}
