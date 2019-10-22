using System;
using System.Linq.Expressions;
using System.Reflection;
using Guts.Data.EntityConfigurations;
using Guts.Domain.AssignmentAggregate;
using Guts.Domain.CourseAggregate;
using Guts.Domain.ExamAggregate;
using Guts.Domain.LoginSessionAggregate;
using Guts.Domain.PeriodAggregate;
using Guts.Domain.ProjectTeamAggregate;
using Guts.Domain.RoleAggregate;
using Guts.Domain.TestAggregate;
using Guts.Domain.TestRunAggregate;
using Guts.Domain.TopicAggregate;
using Guts.Domain.TopicAggregate.ChapterAggregate;
using Guts.Domain.TopicAggregate.ProjectAggregate;
using Guts.Domain.UserAggregate;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;

namespace Guts.Data
{
    public class GutsContext : IdentityDbContext<User, Role, int>
    {
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
        public DbSet<TestResult> TestResults { get; set; }

        public DbSet<Exam> Exams { get; set; }

        public DbSet<ExamPart> ExamParts { get; set; }

        public DbSet<LoginSession> LoginSessions { get; set; }

        public GutsContext(DbContextOptions<GutsContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Topic>().ToTable("Topics");
            builder.Entity<Assignment>().ToTable("Assignments");
            builder.Entity<ProjectTeamUser>().ToTable("ProjectTeamUsers");
            builder.Entity<User>().ToTable("Users");
            builder.Entity<Role>().ToTable("Roles");
            builder.Entity<IdentityUserClaim<int>>().ToTable("UserClaims");
            builder.Entity<IdentityUserRole<int>>().ToTable("UserRoles");
            builder.Entity<IdentityUserLogin<int>>().ToTable("UserLogins");
            builder.Entity<IdentityRoleClaim<int>>().ToTable("RoleClaims");
            builder.Entity<IdentityUserToken<int>>().ToTable("UserTokens");

            builder.ApplyConfiguration(new TestResultConfiguration());
            builder.ApplyConfiguration(new TestConfiguration());
            builder.ApplyConfiguration(new ExamPartConfiguration());
            builder.ApplyConfiguration(new AssignmentEvaluationConfiguration());
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            //Make sure datetimes returned from the database are UTC datetimes
            optionsBuilder.ReplaceService<IEntityMaterializerSource, UtcAwareEntityMaterializerSource>();
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer(@"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=Guts;Integrated Security=True;", sqlOptions =>
                {
                    sqlOptions.MigrationsAssembly("Guts.Data");
                });
            }
        }

    }

    public static class DateTimeMapper
    {
        public static DateTime SetKindToUtc(DateTime value)
        {
            return DateTime.SpecifyKind(value, DateTimeKind.Utc);
        }
    }

    public class UtcAwareEntityMaterializerSource : EntityMaterializerSource
    {
        private static readonly MethodInfo SetKindToUtcMethod = typeof(DateTimeMapper).GetTypeInfo().GetMethod(nameof(DateTimeMapper.SetKindToUtc));

        public UtcAwareEntityMaterializerSource(EntityMaterializerSourceDependencies dependencies) : base(dependencies)
        {
        }

        public override Expression CreateReadValueExpression(Expression valueBuffer, Type type, int index, IPropertyBase property)
        {
            if (type == typeof(DateTime))
            {
                return Expression.Call(
                    SetKindToUtcMethod,
                    base.CreateReadValueExpression(valueBuffer, type, index, property)
                );
            }
            return base.CreateReadValueExpression(valueBuffer, type, index, property);
        }

       
    }
}
