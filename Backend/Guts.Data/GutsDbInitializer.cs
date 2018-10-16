using System;
using System.Linq;
using System.Threading.Tasks;
using Guts.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Guts.Data
{
    public class GutsDbInitializer
    {
        private readonly GutsContext _context;
        private readonly ILogger _logger;
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<Role> _roleManager;

        public GutsDbInitializer(GutsContext context, ILogger logger, UserManager<User> userManager, RoleManager<Role> roleManager)
        {
            _context = context;
            _logger = logger;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public void DoAutomaticMigrations()
        {
            var pendingMigrations = _context.Database.GetPendingMigrations().ToList();
            if (!pendingMigrations.Any()) return;

            try
            {
                _context.Database.Migrate();
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

            var testPeriod = new Period
            {
                Description = "Test period",
                From = new DateTime(2017, 11, 1),
                Until = new DateTime(2018, 2, 1)
            };
            _context.Periods.AddIfNotExists(p => p.Description, testPeriod);

            _context.SaveChanges();
        }

        private async Task AddRolesIfNotExists()
        {
            if (_roleManager.RoleExistsAsync(Role.Constants.Student).Result) return;

            await _roleManager.CreateAsync(new Role { Name = Role.Constants.Student, NormalizedName = Role.Constants.Student.ToUpper() });
            await _roleManager.CreateAsync(new Role { Name = Role.Constants.Lector, NormalizedName = Role.Constants.Lector.ToUpper() });

            //link exsting students to the "student" role
            var students = _context.Users.Where(u => u.Email.ToLower().EndsWith("@student.pxl.be")).ToList();
            foreach (var student in students)
            {
                await _userManager.AddToRoleAsync(student, Role.Constants.Student);
            }

            //link exsting lectors to the "lector" role
            var lectors = _context.Users.Where(u => u.Email.ToLower().EndsWith("@pxl.be")).ToList();
            foreach (var lector in lectors)
            {
                await _userManager.AddToRoleAsync(lector, Role.Constants.Lector);
            }
        }

    }
}