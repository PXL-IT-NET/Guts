using Guts.Domain.GamificationAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Guts.Infrastructure.EntityConfigurations;

internal class CourseClassGroupConfiguration : IEntityTypeConfiguration<CourseClassGroup>
{
    public void Configure(EntityTypeBuilder<CourseClassGroup> builder)
    {
        builder.ToTable("CourseClassGroups");

        builder.Property(ccg => ccg.Name).HasMaxLength(50).IsRequired();

        builder.HasIndex(ccg => new { ccg.CourseId, ccg.PeriodId, ccg.Name }).IsUnique();

        builder.HasOne(ccg => ccg.Period)
            .WithMany()
            .HasForeignKey(ccg => ccg.PeriodId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}