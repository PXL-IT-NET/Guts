using System.Collections.Generic;
using Guts.Domain.ExamAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Guts.Infrastructure.EntityConfigurations
{
    internal class ExamPartConfiguration : IEntityTypeConfiguration<ExamPart>
    {
        public void Configure(EntityTypeBuilder<ExamPart> builder)
        {
            builder.HasMany(e => (IReadOnlyCollection<AssignmentEvaluation>)e.AssignmentEvaluations).WithOne()
                .HasForeignKey(ea => ea.ExamPartId).OnDelete(DeleteBehavior.NoAction);
            builder.Metadata.FindNavigation(nameof(ExamPart.AssignmentEvaluations))
                .SetPropertyAccessMode(PropertyAccessMode.Field);
        }
    }
}