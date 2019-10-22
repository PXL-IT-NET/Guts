using Guts.Domain.ExamAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Guts.Data.EntityConfigurations
{
    internal class ExamPartConfiguration : IEntityTypeConfiguration<ExamPart>
    {
        public void Configure(EntityTypeBuilder<ExamPart> builder)
        {
            builder.Metadata.FindNavigation(nameof(ExamPart.AssignmentEvaluations))
                .SetPropertyAccessMode(PropertyAccessMode.Field);
            builder.HasOne<Exam>().WithMany(e => e.Parts).HasForeignKey(e => e.ExamId);
            builder.HasMany(e => e.AssignmentEvaluations).WithOne()
                .HasForeignKey(ea => ea.ExamPartId).OnDelete(DeleteBehavior.NoAction);
        }
    }
}