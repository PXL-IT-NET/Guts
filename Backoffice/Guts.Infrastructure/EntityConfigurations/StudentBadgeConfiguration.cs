using Guts.Domain.GamificationAggregate;
using Guts.Domain.GamificationAggregate.Profiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Guts.Infrastructure.EntityConfigurations;

internal class StudentBadgeConfiguration : IEntityTypeConfiguration<StudentBadge>
{
    public void Configure(EntityTypeBuilder<StudentBadge> builder)
    {
        builder.ToTable("StudentBadges");

        builder.Property(sb => sb.Badge)
            .HasConversion(
                b => b.Value,
                value => Badge.FromValue(value))
            .HasColumnName("BadgeType")
            .IsRequired();

        builder.HasIndex(sb => new { sb.StudentCourseProfileId, sb.Badge }).IsUnique();
    }
}
