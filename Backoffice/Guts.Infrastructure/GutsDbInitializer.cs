using System;
using System.Linq;
using System.Threading.Tasks;
using Guts.Domain.CourseAggregate;
using Guts.Domain.RoleAggregate;
using Guts.Domain.UserAggregate;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Polly;

namespace Guts.Infrastructure;

internal class GutsDbInitializer
{
    private readonly GutsContext _context;
    private readonly ILogger _logger;
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<Role> _roleManager;
    private readonly IConfiguration _configuration;

    public GutsDbInitializer(
        GutsContext context,
        ILogger logger,
        UserManager<User> userManager,
        RoleManager<Role> roleManager,
        IConfiguration configuration = null)
    {
        _context = context;
        _logger = logger;
        _userManager = userManager;
        _roleManager = roleManager;
        _configuration = configuration;
    }

    public void DoAutomaticMigrations()
    {
        var pendingMigrations = _context.Database.GetPendingMigrations().ToList();
        if (pendingMigrations.Count == 0)
        {
            _logger.LogInformation("No pending migrations found.");
            return;
        }

        _logger.LogInformation("Migrating database...");

        try
        {

            var retry = Policy.Handle<SqlException>()
                .WaitAndRetry(new TimeSpan[]
                {
                    TimeSpan.FromSeconds(3),
                    TimeSpan.FromSeconds(5),
                    TimeSpan.FromSeconds(8),
                });
            retry.Execute(() => _context.Database.Migrate());

            _logger.LogInformation("Migration succeeded");
        }
        catch (Exception e)
        {
            _logger.LogCritical(e, "Error when trying to migrate database.");
        }
    }

    public void Seed()
    {
        AddRolesIfNotExists().Wait();

        var dotNetEssentialsCourse = new Course
        {
            Code = "dotNet1",
            Name = ".NET Essentials"
        };
        _context.Courses.AddIfNotExists(c => c.Code, dotNetEssentialsCourse);

        var dotAdvancedCourse = new Course
        {
            Code = "dotNet2",
            Name = ".NET Advanced"
        };
        _context.Courses.AddIfNotExists(c => c.Code, dotAdvancedCourse);

        _context.SaveChanges();
    }

    private async Task AddRolesIfNotExists()
    {
        if (!await _roleManager.RoleExistsAsync(Role.Constants.Student))
        {
            await _roleManager.CreateAsync(new Role { Name = Role.Constants.Student, NormalizedName = Role.Constants.Student.ToUpper() });
            var students = await _context.Users.Where(u => u.Email.ToLower().EndsWith("@student.pxl.be")).ToListAsync();
            foreach (var student in students)
            {
                await _userManager.AddToRoleAsync(student, Role.Constants.Student);
            }
        }

        if (!await _roleManager.RoleExistsAsync(Role.Constants.Lector))
        {
            await _roleManager.CreateAsync(new Role { Name = Role.Constants.Lector, NormalizedName = Role.Constants.Lector.ToUpper() });
            var lectors = await _context.Users.Where(u => u.Email.ToLower().EndsWith("@pxl.be")).ToListAsync();
            foreach (var lector in lectors)
            {
                await _userManager.AddToRoleAsync(lector, Role.Constants.Lector);
            }
        }

        if (!await _roleManager.RoleExistsAsync(Role.Constants.Admin))
        {
            await _roleManager.CreateAsync(new Role { Name = Role.Constants.Admin, NormalizedName = Role.Constants.Admin.ToUpper() });
        }

        string[] adminEmails = _configuration?.GetSection("AdminEmails").Get<string[]>() ?? [];
        foreach (string email in adminEmails.Where(e => !string.IsNullOrWhiteSpace(e)))
        {
            string normalizedEmail = email.Trim().ToLower();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);
            if (user != null && !await _userManager.IsInRoleAsync(user, Role.Constants.Admin))
            {
                await _userManager.AddToRoleAsync(user, Role.Constants.Admin);
            }
        }
    }

}