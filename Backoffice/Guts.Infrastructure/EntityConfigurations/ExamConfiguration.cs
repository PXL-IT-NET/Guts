using System.Collections.Generic;
using Guts.Domain.ExamAggregate;
using Guts.Domain.PeriodAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Guts.Infrastructure.EntityConfigurations
{
    internal class ExamConfiguration : IEntityTypeConfiguration<Exam>
    {
        public void Configure(EntityTypeBuilder<Exam> builder)
        {
            builder.HasOne(e => (Period)e.Period).WithMany().HasForeignKey(e => e.PeriodId);
            builder.HasMany(e => (IReadOnlyCollection<ExamPart>)e.Parts).WithOne().HasForeignKey(ep => ep.ExamId);
            builder.Metadata.FindNavigation(nameof(Exam.Parts)).SetPropertyAccessMode(PropertyAccessMode.Field);
        }
    }
}