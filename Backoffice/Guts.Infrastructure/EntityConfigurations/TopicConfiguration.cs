using Guts.Domain.AssignmentAggregate;
using Guts.Domain.TopicAggregate;
using Guts.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Guts.Infrastructure.EntityConfigurations;

internal class TopicConfiguration : IEntityTypeConfiguration<Topic>
{
    public void Configure(EntityTypeBuilder<Topic> builder)
    {
        builder.ToTable("Topics");
        builder.Property(x => x.Code).IsRequired().HasMaxLength(64).HasConversion(e => e.ToString(), c => new Code(c));
        builder.Property(x => x.Description).IsRequired();
    }
}