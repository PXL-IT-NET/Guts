using Guts.Domain.AssignmentAggregate;
using Guts.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Guts.Infrastructure.EntityConfigurations
{
    internal class SolutionFileConfiguration : IEntityTypeConfiguration<SolutionFile>
    {
        public void Configure(EntityTypeBuilder<SolutionFile> builder)
        {
            builder.ToTable("SolutionFiles");
            builder.HasKey(sf => new {sf.AssignmentId, sf.UserId, sf.ModifyDateTime, FileName = sf.FilePath});

            builder.HasOne<Assignment>().WithMany().HasForeignKey(sf => sf.AssignmentId);

            builder.HasOne(sf => sf.User).WithMany().HasForeignKey(sf => sf.UserId);

            builder.Property(sf => sf.FilePath).HasConversion(f => f.FullPath, s => new FilePath(s));
            builder.Property(sf => sf.FilePath).HasMaxLength(255);
        }
    }
}