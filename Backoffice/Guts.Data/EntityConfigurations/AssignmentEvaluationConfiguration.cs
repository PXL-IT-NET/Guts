using Guts.Domain.AssignmentAggregate;
using Guts.Domain.ExamAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Guts.Data.EntityConfigurations
{
    internal class AssignmentEvaluationConfiguration : IEntityTypeConfiguration<AssignmentEvaluation>
    {
        public void Configure(EntityTypeBuilder<AssignmentEvaluation> builder)
        {
            builder.HasOne(typeof(Assignment)).WithOne();
        }
    }
}