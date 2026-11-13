using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Guts.Api.Controllers;
using Guts.Api.Tests.Builders;
using Guts.Application;
using Guts.Application.Dtos;
using Guts.Application.Security;
using Guts.Application.Services;
using Guts.Common;
using Guts.Domain.RoleAggregate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;

namespace Guts.Api.Tests.Controllers;

[TestFixture]
public class ImpersonationControllerTests
{
    private Mock<IImpersonationService> _serviceMock;
    private ImpersonationController _controller;

    [SetUp]
    public void SetUp()
    {
        _serviceMock = new Mock<IImpersonationService>();
        _controller = new ImpersonationController(_serviceMock.Object)
        {
            ControllerContext = new ControllerContextBuilder().WithUser("1").WithRole(Role.Constants.Lector).Build()
        };
    }

    [Test]
    public void Controller_ShouldHaveLectorOrAdminPolicy()
    {
        var attr = typeof(ImpersonationController).GetCustomAttributes(typeof(AuthorizeAttribute), true)[0] as AuthorizeAttribute;
        Assert.That(attr, Is.Not.Null);
        Assert.That(attr.Policy, Is.EqualTo(ApiConstants.LectorOrAdminPolicy));
    }

    [Test]
    public async Task SearchImpersonatableUsers_ReturnsOk()
    {
        IReadOnlyList<ImpersonatableUserDto> users = [new ImpersonatableUserDto { Id = 5, Email = "stu@student.pxl.be" }];
        _serviceMock.Setup(s => s.SearchImpersonatableUsersAsync("stu", It.IsAny<IReadOnlyCollection<string>>()))
            .ReturnsAsync(users);

        var result = await _controller.SearchImpersonatableUsers("stu") as OkObjectResult;

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Value, Is.SameAs(users));
    }

    [Test]
    public async Task Impersonate_Valid_ReturnsOkWithTokenAccessPass()
    {
        var pass = new TokenAccessPass { Token = "abc", Expiration = DateTime.UtcNow.AddHours(1) };
        _serviceMock.Setup(s => s.ImpersonateAsync(1, It.IsAny<IReadOnlyCollection<string>>(), false, 5))
            .ReturnsAsync(pass);

        var result = await _controller.Impersonate(5) as OkObjectResult;

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Value, Is.SameAs(pass));
    }

    [Test]
    public async Task Impersonate_ContractException_ReturnsBadRequest()
    {
        _serviceMock.Setup(s => s.ImpersonateAsync(1, It.IsAny<IReadOnlyCollection<string>>(), false, 5))
            .ThrowsAsync(new ContractException("Not allowed"));

        var result = await _controller.Impersonate(5);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task Impersonate_NotFound_ReturnsNotFound()
    {
        _serviceMock.Setup(s => s.ImpersonateAsync(1, It.IsAny<IReadOnlyCollection<string>>(), false, 5))
            .ThrowsAsync(new DataNotFoundException());

        var result = await _controller.Impersonate(5);

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }
}
