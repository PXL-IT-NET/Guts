using System;
using Guts.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Guts.Data
{
    public class GutsContext : IdentityDbContext<User, Role, int>
    {
        public DbSet<Period> Periods { get; set; }
        public DbSet<Course> Courses { get; set; }
        public DbSet<Chapter> Chapters { get; set; }
        public DbSet<Exercise> Exercises { get; set; }
        public DbSet<Test> Tests { get; set; }
        public DbSet<TestRun> TestRuns { get; set; }
        public DbSet<TestResult> TestResults { get; set; }

        public GutsContext(DbContextOptions<GutsContext> options) : base(options)
        {
            var contextServices = ((IInfrastructure<IServiceProvider>)this).Instance;
            var loggerFactory = contextServices.GetRequiredService<ILoggerFactory>();
            loggerFactory.AddConsole(LogLevel.Debug);
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

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

        public void Seed()
        {
            var dotNetEssentialsCourse = new Course
            {
                Code = "dotNet1",
                Name = ".NET Essentials"
            };
            Courses.AddIfNotExists(c => c.Code, dotNetEssentialsCourse);

            var testPeriod = new Period
            {
                Description = "Test period",
                From = new DateTime(2017, 11, 1),
                Until = new DateTime(2018, 2, 1)
            };
            Periods.AddIfNotExists(p => p.Description, testPeriod);

            SaveChanges();
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
