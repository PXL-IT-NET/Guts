using Guts.Domain.ExamAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Guts.Infrastructure.EntityConfigurations;

internal class ExamQuestionConfiguration : IEntityTypeConfiguration<ExamQuestion>
{
    public void Configure(EntityTypeBuilder<ExamQuestion> builder)
    {
        builder.ToTable("CodingExamQuestions");
        builder.HasKey(q => q.Id);

        builder.Property(q => q.ExamId).IsRequired();
        builder.Property(q => q.Title)
            .IsRequired()
            .HasMaxLength(ExamQuestion.MaxTitleLength);
        builder.Property(q => q.InstructionsMarkdown)
            .IsRequired();
        builder.Property(q => q.OrderIndex).IsRequired();
        builder.Property(q => q.TemplateKey)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasMany(q => q.Files)
            .WithOne()
            .HasForeignKey(f => f.ExamQuestionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(q => q.Files)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(q => new { q.ExamId, q.OrderIndex });
    }
}
