using System.Collections.Generic;
using Guts.Domain.TopicAggregate.ProjectAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Guts.Infrastructure.EntityConfigurations;

internal class ProjectAssessmentConfiguration : IEntityTypeConfiguration<ProjectAssessment>
{
    public void Configure(EntityTypeBuilder<ProjectAssessment> builder)
    {
        builder.ToTable("ProjectAssessments");
        builder.HasOne<Project>().WithMany(p => (ICollection<ProjectAssessment>)p.Assessments).HasForeignKey(pa => pa.ProjectId).IsRequired();
        builder.Property(pa => pa.Description).IsRequired();
    }
}