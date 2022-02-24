using Guts.Domain.TopicAggregate;
using Guts.Domain.TopicAggregate.ProjectAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Collections.Generic;

namespace Guts.Infrastructure.EntityConfigurations
{
    internal class ProjectAssessmentConfiguration : IEntityTypeConfiguration<ProjectAssessment>
    {
        public void Configure(EntityTypeBuilder<ProjectAssessment> builder)
        {
            builder.ToTable("ProjectAssessments");
            builder.HasOne<Project>().WithMany(p => (ICollection<ProjectAssessment>)p.Assessments).HasForeignKey(pa => pa.ProjectId).IsRequired();
            builder.Property(pa => pa.Description).IsRequired();
        }
    }
}