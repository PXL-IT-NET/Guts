using System;
using System.Linq;
using Guts.Domain.AssignmentAggregate;
using Guts.Domain.CourseAggregate;
using Guts.Domain.ExamAggregate;
using Guts.Domain.LoginSessionAggregate;
using Guts.Domain.PeriodAggregate;
using Guts.Domain.ProjectTeamAggregate;
using Guts.Domain.ProjectTeamAssessmentAggregate;
using Guts.Domain.RoleAggregate;
using Guts.Domain.TestAggregate;
using Guts.Domain.TestRunAggregate;
using Guts.Domain.TopicAggregate;
using Guts.Domain.TopicAggregate.ChapterAggregate;
using Guts.Domain.TopicAggregate.ProjectAggregate;
using Guts.Domain.UserAggregate;
using Guts.Domain.ValueObjects;
using Guts.Infrastructure.EntityConfigurations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Guts.Infrastructure
{
    internal class GutsContext : IdentityDbContext<User, Role, int>
    {
        private static readonly ValueConverter<DateTime, DateTime> UtcDateTimeConverter =
            new(
                value => value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime(),
                value => DateTime.SpecifyKind(value, DateTimeKind.Utc));

        private static readonly ValueConverter<DateTime?, DateTime?> NullableUtcDateTimeConverter =
            new(
                value => value.HasValue
                    ? value.Value.Kind == DateTimeKind.Utc
                        ? value
                        : value.Value.ToUniversalTime()
                    : value,
                value => value.HasValue
                    ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc)
                    : value);

        public DbSet<Period> Periods { get; set; }
        public DbSet<Course> Courses { get; set; }
        public DbSet<Topic> Topics { get; set; }
        public DbSet<Chapter> Chapters { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<ProjectTeam> ProjectTeams { get; set; }
        public DbSet<ProjectTeamUser> ProjectTeamUsers { get; set; }
        public DbSet<Assignment> Assignments { get; set; }
        public DbSet<Test> Tests { get; set; }
        public DbSet<TestRun> TestRuns { get; set; }
        public DbSet<SolutionFile> SolutionFiles { get; set; }
        public DbSet<TestResult> TestResults { get; set; }
        public DbSet<Exam> Exams { get; set; }
        public DbSet<ExamPart> ExamParts { get; set; }
        public DbSet<LoginSession> LoginSessions { get; set; }

        public DbSet<ProjectTeamAssessment> ProjectTeamAssessments { get; set; }
        public DbSet<PeerAssessment> PeerAssessments { get; set; }

        public GutsContext(DbContextOptions<GutsContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<ProjectTeamUser>().ToTable("ProjectTeamUsers");
            builder.Entity<User>().ToTable("Users");
            builder.Entity<Role>().ToTable("Roles");
            builder.Entity<IdentityUserClaim<int>>().ToTable("UserClaims");
            builder.Entity<IdentityUserRole<int>>().ToTable("UserRoles");
            builder.Entity<IdentityUserLogin<int>>().ToTable("UserLogins");
            builder.Entity<IdentityRoleClaim<int>>().ToTable("RoleClaims");
            builder.Entity<IdentityUserToken<int>>().ToTable("UserTokens");

            builder.ApplyConfiguration(new TopicConfiguration());
            builder.ApplyConfiguration(new AssignmentConfiguration());
            builder.ApplyConfiguration(new SolutionFileConfiguration());
            builder.ApplyConfiguration(new TestResultConfiguration());
            builder.ApplyConfiguration(new TestConfiguration());
            builder.ApplyConfiguration(new ExamConfiguration());
            builder.ApplyConfiguration(new ExamPartConfiguration());
            builder.ApplyConfiguration(new AssignmentEvaluationConfiguration());
            builder.ApplyConfiguration(new ProjectConfiguration());
            builder.ApplyConfiguration(new ProjectTeamConfiguration());
            builder.ApplyConfiguration(new ProjectAssessmentConfiguration());
            builder.ApplyConfiguration(new ProjectTeamAssessmentConfiguration());
            builder.ApplyConfiguration(new PeerAssessmentConfiguration());

            ApplyUtcDateTimeConverters(builder);
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer(@"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=Guts;Integrated Security=True;", sqlOptions =>
                {
                    sqlOptions.MigrationsAssembly("Guts.Infrastructure");
                });
            }
        }

        private static void ApplyUtcDateTimeConverters(ModelBuilder builder)
        {
            foreach (var property in builder.Model.GetEntityTypes().SelectMany(entityType => entityType.GetProperties()))
            {
                if (property.ClrType == typeof(DateTime))
                {
                    property.SetValueConverter(UtcDateTimeConverter);
                }
                else if (property.ClrType == typeof(DateTime?))
                {
                    property.SetValueConverter(NullableUtcDateTimeConverter);
                }
            }
        }

    }
}
