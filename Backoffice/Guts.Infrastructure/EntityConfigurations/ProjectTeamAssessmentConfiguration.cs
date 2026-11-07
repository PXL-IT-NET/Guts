using System.Collections.Generic;
using Guts.Domain.ProjectTeamAggregate;
using Guts.Domain.ProjectTeamAssessmentAggregate;
using Guts.Domain.TopicAggregate.ProjectAggregate;
using Guts.Domain.UserAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Guts.Infrastructure.EntityConfigurations;

internal class ProjectTeamAssessmentConfiguration : IEntityTypeConfiguration<ProjectTeamAssessment>
{
    public void Configure(EntityTypeBuilder<ProjectTeamAssessment> builder)
    {
        builder.ToTable("ProjectTeamAssessments");

        builder.HasOne(pta => (ProjectAssessment)pta.ProjectAssessment)
            .WithMany()
            .HasForeignKey(nameof(ProjectTeamAssessment.ProjectAssessment) + "Id")
            .IsRequired();

        builder.HasOne(pta => (ProjectTeam)pta.Team).WithMany()
            .HasForeignKey(nameof(ProjectTeam) + "Id")
            .OnDelete(DeleteBehavior.NoAction)
            .IsRequired();

        builder.HasMany(pta => pta.Peers)
            .WithMany()
            .UsingEntity<Dictionary<string, object>>(
                "ProjectTeamAssessmentPeers",
                j => j.HasOne<User>().WithMany().HasForeignKey("UserId").OnDelete(DeleteBehavior.Cascade),
                j => j.HasOne<ProjectTeamAssessment>().WithMany().HasForeignKey("ProjectTeamAssessmentId").OnDelete(DeleteBehavior.Cascade));

        builder.Navigation(pta => pta.Peers).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(pta => (IReadOnlyCollection<PeerAssessment>)pta.PeerAssessments)
            .WithOne()
            .HasForeignKey(pa => pa.ProjectTeamAssessmentId)
            .IsRequired();

        builder.Metadata.FindNavigation(nameof(ProjectTeamAssessment.PeerAssessments)).SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}