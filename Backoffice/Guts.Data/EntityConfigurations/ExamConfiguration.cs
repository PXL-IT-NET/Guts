using Guts.Domain.ExamAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Guts.Data.EntityConfigurations
{
    internal class ExamConfiguration : IEntityTypeConfiguration<Exam>
    {
        public void Configure(EntityTypeBuilder<Exam> builder)
        {
            builder.Metadata.FindNavigation(nameof(Exam.Parts))
                .SetPropertyAccessMode(PropertyAccessMode.Field);
            builder.HasMany(e => e.Parts).WithOne().HasForeignKey(p => p.ExamId);
        }
    }
}