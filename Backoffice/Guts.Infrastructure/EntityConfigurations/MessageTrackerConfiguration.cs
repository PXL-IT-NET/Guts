using Guts.Domain.MessageTrackerAggregate;
using Guts.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Guts.Infrastructure.EntityConfigurations;

internal class MessageTrackerConfiguration : IEntityTypeConfiguration<MessageTracker>
{
    public void Configure(EntityTypeBuilder<MessageTracker> builder)
    {
        builder.ToTable("MessageTrackers");

        builder.HasKey(mt => new { mt.EntityType, mt.EntityId, mt.Email });

        builder.Property(mt => mt.EntityType)
            .IsRequired()
            .HasMaxLength(MessageTracker.MaxEntityTypeLength);

        builder.Property(mt => mt.EntityId)
            .IsRequired();

        builder.Property(mt => mt.Email)
            .IsRequired()
            .HasMaxLength(Email.MaxLength)
            .HasConversion(e => e.ToString(), str => new Email(str));

        builder.Property(mt => mt.Status)
            .IsRequired();

        builder.Property(mt => mt.CreatedAtUtc)
            .IsRequired();

        builder.Property(mt => mt.SentAtUtc)
            .IsRequired();

        builder.Property(mt => mt.RetryCount)
            .IsRequired();

        builder.Property(mt => mt.FailureReason)
            .HasMaxLength(1024);
    }
}
