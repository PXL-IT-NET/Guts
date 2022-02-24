using Guts.Domain.TopicAggregate;
using Guts.Domain.TopicAggregate.ProjectAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Collections.Generic;

namespace Guts.Infrastructure.EntityConfigurations
{
    internal class ProjectConfiguration : IEntityTypeConfiguration<Project>
    {
        public void Configure(EntityTypeBuilder<Project> builder)
        {
            builder.HasMany(p => (ICollection<ProjectAssessment>)p.Assessments).WithOne().HasForeignKey(pa => pa.ProjectId);
        }
    }
}