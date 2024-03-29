﻿using System.Collections.Generic;
using Guts.Domain.ProjectTeamAggregate;
using Guts.Domain.TopicAggregate.ProjectAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Guts.Infrastructure.EntityConfigurations;

internal class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.HasMany(p => (ICollection<ProjectAssessment>)p.Assessments).WithOne().HasForeignKey(pa => pa.ProjectId);
        builder.HasMany(p => (ICollection<ProjectTeam>)p.Teams).WithOne(pt => (Project)pt.Project).HasForeignKey(pa => pa.ProjectId);
    }
}