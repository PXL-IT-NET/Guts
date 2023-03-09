using Guts.Domain.ProjectTeamAssessmentAggregate;
using Guts.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Guts.Infrastructure.EntityConfigurations;

internal class PeerAssessmentConfiguration : IEntityTypeConfiguration<PeerAssessment>
{
    public void Configure(EntityTypeBuilder<PeerAssessment> builder)
    {
        builder.ToTable("PeerAssessments");

        builder.HasOne(pa => pa.User)
            .WithMany()
            .HasForeignKey(nameof(PeerAssessment.User) + "Id")
            .OnDelete(DeleteBehavior.NoAction)
            .IsRequired();
        builder.HasOne(pa => pa.Subject).WithMany().HasForeignKey(nameof(PeerAssessment.Subject) + "Id").IsRequired();

        builder.Property(pa => pa.CooperationScore).IsRequired().HasConversion(score => score.Value, scoreValue => new AssessmentScore(scoreValue));
        builder.Property(pa => pa.ContributionScore).IsRequired().HasConversion(score => score.Value, scoreValue => new AssessmentScore(scoreValue));
        builder.Property(pa => pa.EffortScore).IsRequired().HasConversion(score => score.Value, scoreValue => new AssessmentScore(scoreValue));
    }
}