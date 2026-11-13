using Guts.Common;
using Guts.Domain.RoleAggregate;
using Guts.Domain.UserAggregate;
using NUnit.Framework;

namespace Guts.Domain.Tests.UserAggregate;

[TestFixture]
public class ImpersonationPolicyTests
{
    [Test]
    public void EnsureCanImpersonate_AdminImpersonatingLector_DoesNotThrow()
    {
        var target = new User { Id = 10, Email = "teacher@pxl.be" };

        Assert.DoesNotThrow(() =>
            ImpersonationPolicy.EnsureCanImpersonate([Role.Constants.Admin], false, target, [Role.Constants.Lector]));
    }

    [Test]
    public void EnsureCanImpersonate_AdminImpersonatingStudent_DoesNotThrow()
    {
        var target = new User { Id = 11, Email = "student@student.pxl.be" };

        Assert.DoesNotThrow(() =>
            ImpersonationPolicy.EnsureCanImpersonate([Role.Constants.Admin], false, target, [Role.Constants.Student]));
    }

    [Test]
    public void EnsureCanImpersonate_LectorImpersonatingStudent_DoesNotThrow()
    {
        var target = new User { Id = 11, Email = "student@student.pxl.be" };

        Assert.DoesNotThrow(() =>
            ImpersonationPolicy.EnsureCanImpersonate([Role.Constants.Lector], false, target, [Role.Constants.Student]));
    }

    [Test]
    public void EnsureCanImpersonate_LectorImpersonatingLector_ThrowsContractException()
    {
        var target = new User { Id = 12, Email = "other@pxl.be" };

        Assert.Throws<ContractException>(() =>
            ImpersonationPolicy.EnsureCanImpersonate([Role.Constants.Lector], false, target, [Role.Constants.Lector]));
    }

    [Test]
    public void EnsureCanImpersonate_LectorImpersonatingAdmin_ThrowsContractException()
    {
        var target = new User { Id = 12, Email = "admin@pxl.be" };

        Assert.Throws<ContractException>(() =>
            ImpersonationPolicy.EnsureCanImpersonate([Role.Constants.Lector], false, target, [Role.Constants.Admin]));
    }

    [Test]
    public void EnsureCanImpersonate_AdminImpersonatingAdmin_ThrowsContractException()
    {
        var target = new User { Id = 12, Email = "admin2@pxl.be" };

        Assert.Throws<ContractException>(() =>
            ImpersonationPolicy.EnsureCanImpersonate([Role.Constants.Admin], false, target, [Role.Constants.Admin]));
    }

    [Test]
    public void EnsureCanImpersonate_AlreadyImpersonating_ThrowsContractException()
    {
        var target = new User { Id = 11, Email = "student@student.pxl.be" };

        Assert.Throws<ContractException>(() =>
            ImpersonationPolicy.EnsureCanImpersonate([Role.Constants.Admin], true, target, [Role.Constants.Student]));
    }

    [Test]
    public void EnsureCanImpersonate_StudentCaller_ThrowsContractException()
    {
        var target = new User { Id = 11, Email = "student@student.pxl.be" };

        Assert.Throws<ContractException>(() =>
            ImpersonationPolicy.EnsureCanImpersonate([Role.Constants.Student], false, target, [Role.Constants.Student]));
    }
}
