using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
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
    public async Task SearchImpersonatableUsersAsync_ForLector_ExcludesLectorsAndAdmins()
    {
        var student = new User { Id = 2, Email = "s@student.pxl.be" };
        var dualRole = new User { Id = 3, Email = "l@pxl.be" };
        var links = new UserCourseLinks(false, false, false, false, false, false);

        _userRepositoryMock
            .Setup(r => r.SearchUsersAsync("", Role.Constants.Student, false, 50))
            .ReturnsAsync(new List<UserWithRolesAndLinks>
            {
                new(student, [Role.Constants.Student], links),
                new(dualRole, [Role.Constants.Student, Role.Constants.Lector], links)
            });

        var result = await _service.SearchImpersonatableUsersAsync("", [Role.Constants.Lector]);

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Id, Is.EqualTo(2));
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
