using Guts.Domain.CourseAggregate;
using Guts.Domain.GamificationAggregate;
using Guts.Domain.PeriodAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Guts.Infrastructure.EntityConfigurations
{
    internal class StudentCourseProfileConfiguration : IEntityTypeConfiguration<StudentCourseProfile>
    {
        public void Configure(EntityTypeBuilder<StudentCourseProfile> builder)
        {
            builder.ToTable("StudentCourseProfiles");

            builder.HasIndex(sp => new { sp.UserId, sp.CourseId, sp.PeriodId }).IsUnique();

            builder.Property(sp => sp.Pseudonym).HasMaxLength(50).IsRequired();
            builder.Property(sp => sp.Avatar).HasMaxLength(50).IsRequired();
            builder.Property(sp => sp.ShowInLeaderBoard).HasDefaultValue(true);
            builder.Property(sp => sp.LastLeaderboardVisitDate).IsRequired(false);

            builder.OwnsOne(sp => sp.WeeklyStreak, streakBuilder =>
            {
                streakBuilder.Property(s => s.CurrentStreak).HasColumnName("CurrentWeeklyStreak");
                streakBuilder.Property(s => s.BestStreak).HasColumnName("BestWeeklyStreak");
                streakBuilder.Property(s => s.LastActiveYear).HasColumnName("StreakLastActiveYear");
                streakBuilder.Property(s => s.LastActiveWeek).HasColumnName("StreakLastActiveWeek");
            });

            builder.HasOne(sp => sp.User)
                .WithMany()
                .HasForeignKey(sp => sp.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne<Course>()
                .WithMany()
                .HasForeignKey(sp => sp.CourseId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne<Period>()
                .WithMany()
                .HasForeignKey(sp => sp.PeriodId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(sp => sp.ClassGroup)
                .WithMany()
                .HasForeignKey(sp => sp.ClassGroupId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasMany(sp => sp.Badges)
                .WithOne(sb => sb.StudentCourseProfile)
                .HasForeignKey(sb => sb.StudentCourseProfileId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(sp => sp.Events)
                .WithOne(ge => ge.StudentCourseProfile)
                .HasForeignKey(ge => ge.StudentCourseProfileId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
