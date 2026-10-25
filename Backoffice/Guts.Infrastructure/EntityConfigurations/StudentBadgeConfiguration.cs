using Guts.Domain.GamificationAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Guts.Infrastructure.EntityConfigurations
{
    internal class StudentBadgeConfiguration : IEntityTypeConfiguration<StudentBadge>
    {
        public void Configure(EntityTypeBuilder<StudentBadge> builder)
        {
            builder.ToTable("StudentBadges");

            builder.HasIndex(sb => new { sb.StudentCourseProfileId, sb.BadgeType }).IsUnique();
        }
    }
}

