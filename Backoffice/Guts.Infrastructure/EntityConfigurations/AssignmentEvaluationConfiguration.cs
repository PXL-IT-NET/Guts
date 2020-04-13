using Guts.Domain.ExamAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Guts.Infrastructure.EntityConfigurations
{
    internal class AssignmentEvaluationConfiguration : IEntityTypeConfiguration<AssignmentEvaluation>
    {
        public void Configure(EntityTypeBuilder<AssignmentEvaluation> builder)
        {
            builder.HasOne(assignmentEvaluation => assignmentEvaluation.Assignment).WithMany()
                .HasForeignKey(assignmentEvaluation => assignmentEvaluation.AssignmentId);
        }
    }
}