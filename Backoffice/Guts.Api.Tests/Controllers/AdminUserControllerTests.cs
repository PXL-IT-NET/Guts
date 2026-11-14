using System.Collections.Generic;
using System.Threading.Tasks;
using Guts.Api.Controllers;
using Guts.Api.Models;
using Guts.Api.Tests.Builders;
using Guts.Application;
using Guts.Application.Dtos;
using Guts.Application.Services;
using Guts.Common;
using Guts.Domain.RoleAggregate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;

namespace Guts.Api.Tests.Controllers;

[TestFixture]
public class AdminUserControllerTests
{
    private Mock<IAdminUserService> _serviceMock;
    private AdminUserController _controller;

    [SetUp]
    public void SetUp()
    {
        _serviceMock = new Mock<IAdminUserService>();
        _controller = new AdminUserController(_serviceMock.Object)
        {
            ControllerContext = new ControllerContextBuilder().WithUser("1").WithRole(Role.Constants.Admin).Build()
        };
    }

    [Test]
    public void Controller_ShouldHaveAdminOnlyPolicy()
    {
        var attr = typeof(AdminUserController).GetCustomAttributes(typeof(AuthorizeAttribute), true)[0] as AuthorizeAttribute;
        Assert.That(attr, Is.Not.Null);
        Assert.That(attr.Policy, Is.EqualTo(ApiConstants.AdminOnlyPolicy));
    }

    [Test]
    public async Task SearchUsers_ReturnsOkWithPagedUsers()
    {
        var paged = new PagedResult<AdminUserDto>
        {
            Items = [new AdminUserDto { Id = 2, Email = "s@student.pxl.be" }],
            TotalCount = 1,
            Page = 1,
            PageSize = 25
        };
        _serviceMock.Setup(s => s.SearchUsersAsync("s", null, false, 1, 1, 25)).ReturnsAsync(paged);

        var result = await _controller.SearchUsers("s", null, false, 1, 25) as OkObjectResult;

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Value, Is.SameAs(paged));
    }

    [Test]
    public async Task SearchUsers_InvalidPage_ReturnsBadRequest()
    {
        var result = await _controller.SearchUsers(page: 0, pageSize: 25);

        Assert.That(result, Is.InstanceOf<BadRequestResult>());
    }

    [Test]
    public async Task DeleteUser_Valid_ReturnsNoContent()
    {
        var result = await _controller.DeleteUser(2);

        Assert.That(result, Is.InstanceOf<NoContentResult>());
        _serviceMock.Verify(s => s.DeleteUserAsync(2, 1), Times.Once);
    }

    [Test]
    public async Task DeleteUser_ContractException_ReturnsBadRequest()
    {
        _serviceMock.Setup(s => s.DeleteUserAsync(2, 1)).ThrowsAsync(new ContractException("Linked"));

        var result = await _controller.DeleteUser(2);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task DeleteUser_NotFound_ReturnsNotFound()
    {
        _serviceMock.Setup(s => s.DeleteUserAsync(2, 1)).ThrowsAsync(new DataNotFoundException());

        var result = await _controller.DeleteUser(2);

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task DeleteUsers_Valid_ReturnsOkWithCount()
    {
        var model = new DeleteUsersModel { UserIds = [2, 3] };
        _serviceMock.Setup(s => s.DeleteUsersAsync(model.UserIds, 1)).ReturnsAsync(2);

        var result = await _controller.DeleteUsers(model) as OkObjectResult;

        Assert.That(result, Is.Not.Null);
        _serviceMock.Verify(s => s.DeleteUsersAsync(model.UserIds, 1), Times.Once);
    }

    [Test]
    public async Task DeleteUsers_EmptySelection_ReturnsBadRequest()
    {
        var result = await _controller.DeleteUsers(new DeleteUsersModel { UserIds = [] });

        Assert.That(result, Is.InstanceOf<BadRequestResult>());
    }

    [Test]
    public async Task DeleteUsers_NotFound_ReturnsNotFound()
    {
        var model = new DeleteUsersModel { UserIds = [2, 999] };
        _serviceMock.Setup(s => s.DeleteUsersAsync(model.UserIds, 1)).ThrowsAsync(new DataNotFoundException());

        var result = await _controller.DeleteUsers(model);

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task DeleteUsers_ContractException_ReturnsBadRequest()
    {
        var model = new DeleteUsersModel { UserIds = [2, 3] };
        _serviceMock.Setup(s => s.DeleteUsersAsync(model.UserIds, 1)).ThrowsAsync(new ContractException("Linked"));

        var result = await _controller.DeleteUsers(model);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task PromoteToAdmin_Valid_ReturnsNoContent()
    {
        var result = await _controller.PromoteToAdmin(10);

        Assert.That(result, Is.InstanceOf<NoContentResult>());
        _serviceMock.Verify(s => s.PromoteToAdminAsync(10), Times.Once);
    }

    [Test]
    public async Task PromoteToAdmin_ContractException_ReturnsBadRequest()
    {
        _serviceMock.Setup(s => s.PromoteToAdminAsync(10)).ThrowsAsync(new ContractException("Not a teacher"));

        var result = await _controller.PromoteToAdmin(10);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task DemoteFromAdmin_LastAdmin_ReturnsBadRequest()
    {
        _serviceMock.Setup(s => s.DemoteFromAdminAsync(1)).ThrowsAsync(new ContractException("Last admin"));

        var result = await _controller.DemoteFromAdmin(1);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }
}
