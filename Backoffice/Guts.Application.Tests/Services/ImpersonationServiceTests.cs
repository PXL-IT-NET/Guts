using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Guts.Application.Dtos;
using Guts.Application.Repositories;
using Guts.Application.Security;
using Guts.Application.Services;
using Guts.Common;
using Guts.Domain.RoleAggregate;
using Guts.Domain.UserAggregate;
using Moq;
using NUnit.Framework;

namespace Guts.Application.Tests.Services;

[TestFixture]
public class ImpersonationServiceTests
{
    private Mock<IUserRepository> _userRepositoryMock;
    private Mock<ITokenAccessPassFactory> _tokenFactoryMock;
    private ImpersonationService _service;

    [SetUp]
    public void SetUp()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _tokenFactoryMock = new Mock<ITokenAccessPassFactory>();
        _service = new ImpersonationService(_userRepositoryMock.Object, _tokenFactoryMock.Object);
    }

    [Test]
    public async Task SearchImpersonatableUsersAsync_ForLector_ExcludesLectorsAndAdminsAndPreservesPaging()
    {
        var student = new User { Id = 2, Email = "s@student.pxl.be", FirstName = "Stu", LastName = "Dent" };
        var dualRole = new User { Id = 3, Email = "l@pxl.be" };
        var links = new UserCourseLinks(false, false, false, false, false, false);

        _userRepositoryMock
            .Setup(r => r.SearchUsersAsync(
                "stu",
                Role.Constants.Student,
                false,
                It.Is<IReadOnlyCollection<string>>(ex =>
                    ex.Contains(Role.Constants.Admin) && ex.Contains(Role.Constants.Lector)),
                2,
                25))
            .ReturnsAsync(new PagedResult<UserWithRolesAndLinks>
            {
                Items = new List<UserWithRolesAndLinks>
                {
                    new(student, [Role.Constants.Student], links),
                    new(dualRole, [Role.Constants.Student, Role.Constants.Lector], links)
                },
                TotalCount = 30,
                Page = 2,
                PageSize = 25
            });

        var result = await _service.SearchImpersonatableUsersAsync("stu", [Role.Constants.Lector], 2, 25);

        Assert.That(result.TotalCount, Is.EqualTo(30));
        Assert.That(result.Page, Is.EqualTo(2));
        Assert.That(result.PageSize, Is.EqualTo(25));
        Assert.That(result.Items, Has.Count.EqualTo(1));
        Assert.That(result.Items[0].Id, Is.EqualTo(2));
    }

    [Test]
    public async Task SearchImpersonatableUsersAsync_ForAdmin_ExcludesOnlyAdminsAndAllowsLectors()
    {
        var lector = new User { Id = 4, Email = "l@pxl.be", FirstName = "Lec", LastName = "Tor" };
        var links = new UserCourseLinks(false, false, false, false, false, false);

        _userRepositoryMock
            .Setup(r => r.SearchUsersAsync(
                null,
                null,
                false,
                It.Is<IReadOnlyCollection<string>>(ex =>
                    ex.Count == 1 && ex.Contains(Role.Constants.Admin)),
                1,
                null))
            .ReturnsAsync(new PagedResult<UserWithRolesAndLinks>
            {
                Items = [new(lector, [Role.Constants.Lector], links)],
                TotalCount = 1,
                Page = 1,
                PageSize = null
            });

        var result = await _service.SearchImpersonatableUsersAsync(null, [Role.Constants.Admin], 1, null);

        Assert.That(result.TotalCount, Is.EqualTo(1));
        Assert.That(result.PageSize, Is.Null);
        Assert.That(result.Items, Has.Count.EqualTo(1));
        Assert.That(result.Items[0].Id, Is.EqualTo(4));
    }

    [Test]
    public async Task ImpersonateAsync_AdminImpersonatingLector_ReturnsTokenWithImpersonatorId()
    {
        var target = new User { Id = 10, Email = "lector@pxl.be" };
        var claims = new List<Claim>();
        IReadOnlyList<string> roles = [Role.Constants.Lector];
        var pass = new TokenAccessPass { Token = "jwt-token", Expiration = DateTime.UtcNow.AddHours(1) };

        _userRepositoryMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(target);
        _userRepositoryMock.Setup(r => r.GetRolesAsync(target)).ReturnsAsync(roles);
        _userRepositoryMock.Setup(r => r.GetClaimsAsync(target)).ReturnsAsync(claims);
        _tokenFactoryMock.Setup(f => f.Create(target, claims, It.IsAny<IList<string>>(), 1)).Returns(pass);

        var result = await _service.ImpersonateAsync(1, [Role.Constants.Admin], false, 10);

        Assert.That(result, Is.SameAs(pass));
        _tokenFactoryMock.Verify(f => f.Create(target, claims, It.IsAny<IList<string>>(), 1), Times.Once);
    }

    [Test]
    public void ImpersonateAsync_LectorImpersonatingLector_ThrowsContractException()
    {
        var target = new User { Id = 10, Email = "lector@pxl.be" };
        _userRepositoryMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(target);
        _userRepositoryMock.Setup(r => r.GetRolesAsync(target)).ReturnsAsync([Role.Constants.Lector]);

        Assert.ThrowsAsync<ContractException>(() =>
            _service.ImpersonateAsync(1, [Role.Constants.Lector], false, 10));
    }

    [Test]
    public void ImpersonateAsync_NestedImpersonation_ThrowsContractException()
    {
        var target = new User { Id = 10, Email = "student@student.pxl.be" };
        _userRepositoryMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(target);
        _userRepositoryMock.Setup(r => r.GetRolesAsync(target)).ReturnsAsync([Role.Constants.Student]);

        Assert.ThrowsAsync<ContractException>(() =>
            _service.ImpersonateAsync(1, [Role.Constants.Admin], true, 10));
    }
}
