using Guts.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Guts.Data
{
    public class GutsContext : IdentityDbContext<User, Role, int>
    {
        public DbSet<Period> Periods { get; set; }
        public DbSet<Course> Courses { get; set; }
        public DbSet<Chapter> Chapters { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<ProjectTeam> ProjectTeams { get; set; }
        public DbSet<ProjectComponent> ProjectComponents { get; set; }
        public DbSet<Exercise> Exercises { get; set; }
        public DbSet<Test> Tests { get; set; }
        public DbSet<TestRun> TestRuns { get; set; }
        public DbSet<TestResult> TestResults { get; set; }

        public GutsContext(DbContextOptions<GutsContext> options) : base(options)
        {
            //var contextServices = ((IInfrastructure<IServiceProvider>)this).Instance;
            //var loggerFactory = contextServices.GetRequiredService<ILoggerFactory>();
            //loggerFactory.AddConsole(LogLevel.Debug);
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Assignment>().ToTable("Assignments");
            builder.Entity<ProjectTeamUser>().ToTable("ProjectTeamUsers");
            builder.Entity<User>().ToTable("Users");
            builder.Entity<Role>().ToTable("Roles");
            builder.Entity<IdentityUserClaim<int>>().ToTable("UserClaims");
            builder.Entity<IdentityUserRole<int>>().ToTable("UserRoles");
            builder.Entity<IdentityUserLogin<int>>().ToTable("UserLogins");
            builder.Entity<IdentityRoleClaim<int>>().ToTable("RoleClaims");
            builder.Entity<IdentityUserToken<int>>().ToTable("UserTokens");

            builder.Entity<TestResult>().HasOne(result => result.Test).WithMany(test => test.Results)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<TestResult>().HasOne(result => result.TestRun).WithMany(run => run.TestResults)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Test>().HasMany(test => test.Results).WithOne(result => result.Test)
                .OnDelete(DeleteBehavior.Cascade);
        }
        
    }

    /// <summary>
    /// Used when creating migrations
    /// </summary>
    public class GutsContextFactory : IDesignTimeDbContextFactory<GutsContext>
    {
        public GutsContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<GutsContext>();
            optionsBuilder.UseMySql("server=localhost;database=Guts;user id=Guts;Password=Q*ED&Yv9nK", sqlOptions =>
            {
                sqlOptions.MigrationsAssembly("Guts.Data");
            });

            return new GutsContext(optionsBuilder.Options);
        }
    }
}
