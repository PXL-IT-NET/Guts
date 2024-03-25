using System.Collections.Generic;
using Guts.Domain.ProjectTeamAggregate;
using Guts.Domain.TopicAggregate.ProjectAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Guts.Infrastructure.EntityConfigurations;

internal class ProjectTeamConfiguration : IEntityTypeConfiguration<ProjectTeam>
{
    public void Configure(EntityTypeBuilder<ProjectTeam> builder)
    {
        builder.ToTable("ProjectTeams");

        builder.Property(t => t.Name).IsRequired(true);

        builder.HasOne(pt => (Project)pt.Project)
            .WithMany(p => (ICollection<ProjectTeam>)p.Teams)
            .HasForeignKey(pt => pt.ProjectId)
            .IsRequired();

        builder.HasMany(pt => (ICollection<ProjectTeamUser>)pt.TeamUsers)
            .WithOne(ptu => (ProjectTeam)ptu.ProjectTeam)
            .HasForeignKey(ptu => ptu.ProjectTeamId)
            .IsRequired();
    }
}