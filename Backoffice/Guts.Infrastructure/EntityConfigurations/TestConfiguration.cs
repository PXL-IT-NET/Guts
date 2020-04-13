using Guts.Domain.TestAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Guts.Infrastructure.EntityConfigurations
{
    internal class TestConfiguration : IEntityTypeConfiguration<Test>
    {
        public void Configure(EntityTypeBuilder<Test> builder)
        {
            builder.HasMany(test => test.Results).WithOne(result => result.Test)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}