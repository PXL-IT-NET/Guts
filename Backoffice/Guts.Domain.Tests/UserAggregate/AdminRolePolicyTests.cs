using Guts.Common;
using Guts.Domain.RoleAggregate;
using Guts.Domain.UserAggregate;
using NUnit.Framework;

namespace Guts.Domain.Tests.UserAggregate;

[TestFixture]
public class AdminRolePolicyTests
{
    [Test]
    public void EnsureCanPromoteToAdmin_LectorNotAdmin_DoesNotThrow()
    {
        Assert.DoesNotThrow(() =>
            AdminRolePolicy.EnsureCanPromoteToAdmin([Role.Constants.Lector]));
    }

    [Test]
    public void EnsureCanPromoteToAdmin_StudentOnly_ThrowsContractException()
    {
        Assert.Throws<ContractException>(() =>
            AdminRolePolicy.EnsureCanPromoteToAdmin([Role.Constants.Student]));
    }

    [Test]
    public void EnsureCanPromoteToAdmin_AlreadyAdmin_ThrowsContractException()
    {
        Assert.Throws<ContractException>(() =>
            AdminRolePolicy.EnsureCanPromoteToAdmin([Role.Constants.Lector, Role.Constants.Admin]));
    }

    [Test]
    public void EnsureCanDemoteFromAdmin_MoreThanOneAdmin_DoesNotThrow()
    {
        Assert.DoesNotThrow(() =>
            AdminRolePolicy.EnsureCanDemoteFromAdmin([Role.Constants.Lector, Role.Constants.Admin], currentAdminCount: 2));
    }

    [Test]
    public void EnsureCanDemoteFromAdmin_LastAdmin_ThrowsContractException()
    {
        Assert.Throws<ContractException>(() =>
            AdminRolePolicy.EnsureCanDemoteFromAdmin([Role.Constants.Lector, Role.Constants.Admin], currentAdminCount: 1));
    }

    [Test]
    public void EnsureCanDemoteFromAdmin_UserIsNotAdmin_ThrowsContractException()
    {
        Assert.Throws<ContractException>(() =>
            AdminRolePolicy.EnsureCanDemoteFromAdmin([Role.Constants.Lector], currentAdminCount: 2));
    }

    [Test]
    public void CanPromoteToAdmin_ShouldReturnTrueOnlyForLectorNotYetAdmin()
    {
        Assert.That(AdminRolePolicy.CanPromoteToAdmin([Role.Constants.Lector]), Is.True);
        Assert.That(AdminRolePolicy.CanPromoteToAdmin([Role.Constants.Student]), Is.False);
        Assert.That(AdminRolePolicy.CanPromoteToAdmin([Role.Constants.Lector, Role.Constants.Admin]), Is.False);
        Assert.That(AdminRolePolicy.CanPromoteToAdmin(null), Is.False);
    }

    [Test]
    public void CanDemoteFromAdmin_ShouldReturnTrueOnlyForAdminWhenMoreThanOneAdminExists()
    {
        Assert.That(AdminRolePolicy.CanDemoteFromAdmin([Role.Constants.Admin], currentAdminCount: 2), Is.True);
        Assert.That(AdminRolePolicy.CanDemoteFromAdmin([Role.Constants.Admin], currentAdminCount: 1), Is.False);
        Assert.That(AdminRolePolicy.CanDemoteFromAdmin([Role.Constants.Lector], currentAdminCount: 2), Is.False);
        Assert.That(AdminRolePolicy.CanDemoteFromAdmin(null, currentAdminCount: 2), Is.False);
    }
}
