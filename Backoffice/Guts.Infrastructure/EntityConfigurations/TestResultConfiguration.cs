using Guts.Domain.TestRunAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Guts.Infrastructure.EntityConfigurations
{
    internal class TestResultConfiguration : IEntityTypeConfiguration<TestResult>
    {
        public void Configure(EntityTypeBuilder<TestResult> builder)
        {
            builder.HasOne(result => result.Test).WithMany(test => test.Results)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(result => result.TestRun).WithMany(run => run.TestResults)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex((result => new { result.TestId, result.UserId, result.CreateDateTime }));
        }
    }
}