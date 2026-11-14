using System.Collections.Generic;
using System.Threading.Tasks;
using Guts.Application.Dtos;
using Guts.Application.Repositories;
using Guts.Application.Services;
using Guts.Common;
using Guts.Domain.RoleAggregate;
using Guts.Domain.UserAggregate;
using Moq;
using NUnit.Framework;

namespace Guts.Application.Tests.Services;

[TestFixture]
public class AdminUserServiceTests
{
    private Mock<IUserRepository> _userRepositoryMock;
    private AdminUserService _service;

    [SetUp]
    public void SetUp()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _service = new AdminUserService(_userRepositoryMock.Object);
    }

    [Test]
    public async Task SearchUsersAsync_MapsCanDeleteAndCanImpersonateCorrectly()
    {
        var OrphanedStudent = new User { Id = 2, Email = "s@student.pxl.be", FirstName = "Stu", LastName = "Dent" };
        var linkedLector = new User { Id = 3, Email = "l@pxl.be", FirstName = "Lec", LastName = "Tor" };
        var adminUser = new User { Id = 1, Email = "a@pxl.be", FirstName = "Ad", LastName = "Min" };

        var noLinks = new UserCourseLinks(false, false, false, false, false, false);
        var hasLinks = new UserCourseLinks(true, false, false, false, false, false);

        var linkedCourse = new LinkedCourseDto(101, "DOTNET", ".NET Programming");

        _userRepositoryMock
            .Setup(r => r.SearchUsersAsync("q", null, false, 2, 25))
            .ReturnsAsync(new PagedResult<UserWithRolesAndLinks>
            {
                Items = new List<UserWithRolesAndLinks>
                {
                    new(OrphanedStudent, [Role.Constants.Student], noLinks),
                    new(linkedLector, [Role.Constants.Lector], hasLinks, [linkedCourse]),
                    new(adminUser, [Role.Constants.Admin], noLinks)
                },
                TotalCount = 42,
                Page = 2,
                PageSize = 25
            });

        var result = await _service.SearchUsersAsync("q", null, false, currentUserId: 1, page: 2, pageSize: 25);

        Assert.That(result.TotalCount, Is.EqualTo(42));
        Assert.That(result.Page, Is.EqualTo(2));
        Assert.That(result.PageSize, Is.EqualTo(25));
        Assert.That(result.Items, Has.Count.EqualTo(3));
        Assert.That(result.Items[0].CanDelete, Is.True);
        Assert.That(result.Items[0].CanImpersonate, Is.True);
        Assert.That(result.Items[0].LinkedCourses, Is.Empty);
        Assert.That(result.Items[1].CanDelete, Is.False);
        Assert.That(result.Items[1].CanImpersonate, Is.True);
        Assert.That(result.Items[1].LinkedCourses, Is.EqualTo(new[] { linkedCourse }));
        Assert.That(result.Items[2].CanDelete, Is.False);
        Assert.That(result.Items[2].CanImpersonate, Is.False);
    }

    [Test]
    public async Task DeleteUserAsync_OrphanedNonAdmin_DeletesUser()
    {
        var user = new User { Id = 10, Email = "old@student.pxl.be" };
        _userRepositoryMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(user);
        _userRepositoryMock.Setup(r => r.GetRolesAsync(user)).ReturnsAsync([Role.Constants.Student]);
        _userRepositoryMock.Setup(r => r.GetCourseLinksAsync(10))
            .ReturnsAsync(new UserCourseLinks(false, false, false, false, false, false));

        await _service.DeleteUserAsync(10, currentUserId: 1);

        _userRepositoryMock.Verify(r => r.DeleteAsync(user), Times.Once);
    }

    [Test]
    public void DeleteUserAsync_LinkedUser_ThrowsContractExceptionAndDoesNotDelete()
    {
        var user = new User { Id = 10, Email = "active@student.pxl.be" };
        _userRepositoryMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(user);
        _userRepositoryMock.Setup(r => r.GetRolesAsync(user)).ReturnsAsync([Role.Constants.Student]);
        _userRepositoryMock.Setup(r => r.GetCourseLinksAsync(10))
            .ReturnsAsync(new UserCourseLinks(false, false, true, false, false, false));

        Assert.ThrowsAsync<ContractException>(() => _service.DeleteUserAsync(10, currentUserId: 1));
        _userRepositoryMock.Verify(r => r.DeleteAsync(It.IsAny<User>()), Times.Never);
    }

    [Test]
    public async Task DeleteUsersAsync_DeletesEligibleUsersAndReturnsCount()
    {
        var u1 = new User { Id = 2, Email = "u1@student.pxl.be" };
        var u2 = new User { Id = 3, Email = "u2@student.pxl.be" };
        var noLinks = new UserCourseLinks(false, false, false, false, false, false);

        _userRepositoryMock
            .Setup(r => r.GetWithRolesAndLinksByIdsAsync(It.IsAny<IReadOnlyCollection<int>>()))
            .ReturnsAsync(new List<UserWithRolesAndLinks>
            {
                new(u1, [Role.Constants.Student], noLinks),
                new(u2, [Role.Constants.Student], noLinks)
            });

        int count = await _service.DeleteUsersAsync([2, 3, 2], currentUserId: 1);

        Assert.That(count, Is.EqualTo(2));
        _userRepositoryMock.Verify(
            r => r.DeleteRangeAsync(It.Is<IReadOnlyCollection<User>>(users => users.Count == 2)),
            Times.Once);
    }

    [Test]
    public void DeleteUsersAsync_WhenAnyUserIsLinked_ThrowsContractExceptionAndDeletesNone()
    {
        var u1 = new User { Id = 2, Email = "u1@student.pxl.be" };
        var u2 = new User { Id = 3, Email = "u2@student.pxl.be" };
        var noLinks = new UserCourseLinks(false, false, false, false, false, false);
        var hasLinks = new UserCourseLinks(true, false, false, false, false, false);

        _userRepositoryMock
            .Setup(r => r.GetWithRolesAndLinksByIdsAsync(It.IsAny<IReadOnlyCollection<int>>()))
            .ReturnsAsync(new List<UserWithRolesAndLinks>
            {
                new(u1, [Role.Constants.Student], noLinks),
                new(u2, [Role.Constants.Student], hasLinks)
            });

        Assert.ThrowsAsync<ContractException>(() => _service.DeleteUsersAsync([2, 3], currentUserId: 1));
        _userRepositoryMock.Verify(r => r.DeleteRangeAsync(It.IsAny<IReadOnlyCollection<User>>()), Times.Never);
    }

    [Test]
    public void DeleteUsersAsync_WhenUserMissing_ThrowsDataNotFoundExceptionAndDeletesNone()
    {
        var u1 = new User { Id = 2, Email = "u1@student.pxl.be" };
        var noLinks = new UserCourseLinks(false, false, false, false, false, false);

        _userRepositoryMock
            .Setup(r => r.GetWithRolesAndLinksByIdsAsync(It.IsAny<IReadOnlyCollection<int>>()))
            .ReturnsAsync(new List<UserWithRolesAndLinks>
            {
                new(u1, [Role.Constants.Student], noLinks)
            });

        Assert.ThrowsAsync<DataNotFoundException>(() => _service.DeleteUsersAsync([2, 99], currentUserId: 1));
        _userRepositoryMock.Verify(r => r.DeleteRangeAsync(It.IsAny<IReadOnlyCollection<User>>()), Times.Never);
    }

    [Test]
    public async Task PromoteToAdminAsync_LectorUser_AddsAdminRole()
    {
        var lector = new User { Id = 10, Email = "teacher@pxl.be" };
        _userRepositoryMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(lector);
        _userRepositoryMock.Setup(r => r.GetRolesAsync(lector)).ReturnsAsync([Role.Constants.Lector]);

        await _service.PromoteToAdminAsync(10);

        _userRepositoryMock.Verify(r => r.AddToRoleAsync(lector, Role.Constants.Admin), Times.Once);
    }

    [Test]
    public void PromoteToAdminAsync_StudentUser_ThrowsContractException()
    {
        var student = new User { Id = 11, Email = "student@student.pxl.be" };
        _userRepositoryMock.Setup(r => r.GetByIdAsync(11)).ReturnsAsync(student);
        _userRepositoryMock.Setup(r => r.GetRolesAsync(student)).ReturnsAsync([Role.Constants.Student]);

        Assert.ThrowsAsync<ContractException>(() => _service.PromoteToAdminAsync(11));
        _userRepositoryMock.Verify(r => r.AddToRoleAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public void DemoteFromAdminAsync_LastAdmin_ThrowsContractException()
    {
        var admin = new User { Id = 1, Email = "admin@pxl.be" };
        _userRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(admin);
        _userRepositoryMock.Setup(r => r.GetRolesAsync(admin)).ReturnsAsync([Role.Constants.Lector, Role.Constants.Admin]);
        _userRepositoryMock.Setup(r => r.CountAdminsAsync()).ReturnsAsync(1);

        Assert.ThrowsAsync<ContractException>(() => _service.DemoteFromAdminAsync(1));
        _userRepositoryMock.Verify(r => r.RemoveFromRoleAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task DemoteFromAdminAsync_MoreThanOneAdmin_RemovesAdminRole()
    {
        var admin = new User { Id = 2, Email = "admin2@pxl.be" };
        _userRepositoryMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(admin);
        _userRepositoryMock.Setup(r => r.GetRolesAsync(admin)).ReturnsAsync([Role.Constants.Lector, Role.Constants.Admin]);
        _userRepositoryMock.Setup(r => r.CountAdminsAsync()).ReturnsAsync(2);

        await _service.DemoteFromAdminAsync(2);

        _userRepositoryMock.Verify(r => r.RemoveFromRoleAsync(admin, Role.Constants.Admin), Times.Once);
    }
}
