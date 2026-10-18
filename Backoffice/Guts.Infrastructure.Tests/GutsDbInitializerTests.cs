using System.Linq;
using System.Threading.Tasks;
using Guts.Domain.RoleAggregate;
using Guts.Domain.UserAggregate;
using Guts.Infrastructure.Tests.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Guts.Infrastructure.Tests;

[TestFixture]
internal class GutsDbInitializerTests : RepositoryTestsBase
{
    [Test]
    public async Task Seed_ShouldCreateRolesAssignUsersAndSeedCourses()
    {
        await Seeder.SeedUserAsync("John", "Doe", "john@student.pxl.be");
        await Seeder.SeedUserAsync("Jane", "Doe", "jane@pxl.be");

        await using var context = CreateDbContext();

        var userStoreMock = new Mock<IUserStore<User>>();
        var userManagerMock = new Mock<UserManager<User>>(
            userStoreMock.Object, null, null, null, null, null, null, null, null);
        userManagerMock
            .Setup(um => um.AddToRoleAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        var roleStoreMock = new Mock<IRoleStore<Role>>();
        var roleManagerMock = new Mock<RoleManager<Role>>(
            roleStoreMock.Object, null, null, null, null);
        roleManagerMock.SetupSequence(rm => rm.RoleExistsAsync(Role.Constants.Student))
            .ReturnsAsync(false)
            .ReturnsAsync(true);
        roleManagerMock.Setup(rm => rm.CreateAsync(It.IsAny<Role>()))
            .ReturnsAsync(IdentityResult.Success);

        var loggerMock = new Mock<ILogger>();

        var initializer = new GutsDbInitializer(
            context,
            loggerMock.Object,
            userManagerMock.Object,
            roleManagerMock.Object);

        initializer.Seed();

        roleManagerMock.Verify(rm => rm.CreateAsync(It.Is<Role>(r => r.Name == Role.Constants.Student)), Times.Once);
        roleManagerMock.Verify(rm => rm.CreateAsync(It.Is<Role>(r => r.Name == Role.Constants.Lector)), Times.Once);
        userManagerMock.Verify(um => um.AddToRoleAsync(It.Is<User>(u => u.Email == "john@student.pxl.be"), Role.Constants.Student), Times.Once);
        userManagerMock.Verify(um => um.AddToRoleAsync(It.Is<User>(u => u.Email == "jane@pxl.be"), Role.Constants.Lector), Times.Once);
        Assert.That(context.Courses.Select(c => c.Code), Is.EquivalentTo(new[] { "dotNet1", "dotNet2" }));

        initializer.Seed();
        roleManagerMock.Verify(rm => rm.CreateAsync(It.IsAny<Role>()), Times.Exactly(2));
        Assert.That(context.Courses.Count(), Is.EqualTo(2));
    }
}

