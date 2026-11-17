using Guts.Domain.ExamAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Guts.Infrastructure.EntityConfigurations;

internal class ExamQuestionFileConfiguration : IEntityTypeConfiguration<ExamQuestionFile>
{
    public void Configure(EntityTypeBuilder<ExamQuestionFile> builder)
    {
        builder.ToTable("CodingExamQuestionFiles");
        builder.HasKey(f => f.Id);

        builder.Property(f => f.ExamQuestionId).IsRequired();
        builder.Property(f => f.RelativePath)
            .IsRequired()
            .HasMaxLength(ExamFilePath.MaxLength)
            .HasConversion(p => p.Value, value => new ExamFilePath(value));
        builder.Property(f => f.Content).IsRequired();
        builder.Property(f => f.Role).IsRequired();

        builder.HasIndex(f => new { f.ExamQuestionId, f.RelativePath }).IsUnique();
    }
}
