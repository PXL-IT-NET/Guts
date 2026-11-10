using Guts.Domain.AssignmentAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Guts.Infrastructure.EntityConfigurations;

internal class AssignmentConfiguration : IEntityTypeConfiguration<Assignment>
{
    public void Configure(EntityTypeBuilder<Assignment> builder)
    {
        builder.ToTable("Assignments");

        builder.HasMany(a => a.TestCodeHashes)
            .WithOne(h => h.Assignment)
            .HasForeignKey(h => h.AssignmentId);

        builder.Navigation(a => a.TestCodeHashes)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(a => a.Tests)
            .WithOne(t => t.Assignment)
            .HasForeignKey(t => t.AssignmentId);

        builder.Navigation(a => a.Tests)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}