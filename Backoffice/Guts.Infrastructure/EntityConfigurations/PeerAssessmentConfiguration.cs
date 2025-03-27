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

        string userIdForeignKeyName = nameof(PeerAssessment.User) + "Id";
        string subjectIdForeignKeyName = nameof(PeerAssessment.Subject) + "Id";

        builder.HasOne(pa => pa.User)
            .WithMany()
            .HasForeignKey(userIdForeignKeyName)
            .OnDelete(DeleteBehavior.NoAction)
            .IsRequired();
        
        builder.HasOne(pa => pa.Subject).WithMany().HasForeignKey(subjectIdForeignKeyName).IsRequired();

        builder.Property(pa => pa.CooperationScore).IsRequired().HasConversion(score => score.Value, scoreValue => new AssessmentScore(scoreValue));
        builder.Property(pa => pa.ContributionScore).IsRequired().HasConversion(score => score.Value, scoreValue => new AssessmentScore(scoreValue));
        builder.Property(pa => pa.EffortScore).IsRequired().HasConversion(score => score.Value, scoreValue => new AssessmentScore(scoreValue));

        builder.HasIndex(
                nameof(PeerAssessment.ProjectTeamAssessmentId), 
                userIdForeignKeyName, 
                subjectIdForeignKeyName).IsUnique();
    }
}