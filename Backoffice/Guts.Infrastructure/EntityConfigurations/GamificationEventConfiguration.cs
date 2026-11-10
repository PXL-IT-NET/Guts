using Guts.Domain.GamificationAggregate;
using Guts.Domain.GamificationAggregate.Profiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Guts.Infrastructure.EntityConfigurations;

internal class GamificationEventConfiguration : IEntityTypeConfiguration<GamificationEvent>
{
    public void Configure(EntityTypeBuilder<GamificationEvent> builder)
    {
        builder.ToTable("GamificationEvents");

        builder.HasIndex(ge => new { ge.StudentCourseProfileId, ge.EventType, ge.TargetId });
    }
}