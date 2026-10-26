using Guts.Domain.GamificationAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Guts.Infrastructure.EntityConfigurations;

internal class CourseGamificationConfigConfiguration : IEntityTypeConfiguration<CourseGamificationConfig>
{
    public void Configure(EntityTypeBuilder<CourseGamificationConfig> builder)
    {
        builder.ToTable("CourseGamificationConfigs");

        builder.HasIndex(cgc => cgc.CourseId).IsUnique();

        builder.Property(cgc => cgc.IsGamificationEnabled).HasDefaultValue(false);
    }
}