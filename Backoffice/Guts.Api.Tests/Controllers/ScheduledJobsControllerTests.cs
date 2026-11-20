using System.Security.Claims;
using System.Threading.Tasks;
using Guts.Api.Controllers;
using Guts.Api.Models;
using Guts.Api.ScheduledJobs;
using Guts.Domain.RoleAggregate;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;

namespace Guts.Api.Tests.Controllers;

[TestFixture]
public class ScheduledJobsControllerTests
{
    private Mock<IQuartzDashboardTicketStore> _ticketStoreMock;
    private Mock<IAuthenticationService> _authServiceMock;
    private ScheduledJobsController _controller;

    [SetUp]
    public void SetUp()
    {
        _ticketStoreMock = new Mock<IQuartzDashboardTicketStore>();
        _authServiceMock = new Mock<IAuthenticationService>();

        var serviceProviderMock = new Mock<System.IServiceProvider>();
        serviceProviderMock
            .Setup(sp => sp.GetService(typeof(IAuthenticationService)))
            .Returns(_authServiceMock.Object);

        var httpContext = new DefaultHttpContext
        {
            RequestServices = serviceProviderMock.Object,
            User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim(ClaimTypes.Role, Role.Constants.Admin)
            ], "Bearer"))
        };
        httpContext.Request.Scheme = "https";
        httpContext.Request.Host = new HostString("guts-api.pxl.be");
        httpContext.Request.PathBase = new PathString("/v1");

        _controller = new ScheduledJobsController(_ticketStoreMock.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            }
        };
    }

    [Test]
    public void CreateDashboardTicket_ShouldReturnOkWithLoginUrl()
    {
        _ticketStoreMock.Setup(s => s.CreateTicket(_controller.User)).Returns("abc123");

        var result = _controller.CreateDashboardTicket() as OkObjectResult;

        Assert.That(result, Is.Not.Null);
        var model = result.Value as QuartzDashboardTicketModel;
        Assert.That(model, Is.Not.Null);
        Assert.That(model.LoginUrl, Is.EqualTo("https://guts-api.pxl.be/v1/api/scheduled-jobs/dashboard-login?ticket=abc123"));
    }

    [Test]
    public async Task LoginToDashboard_WhenTicketValid_ShouldSignInCookieAndRedirectToQuartz()
    {
        var dashboardPrincipal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, "1"),
            new Claim(ClaimTypes.Role, Role.Constants.Admin)
        ], ApiConstants.QuartzDashboardCookieScheme));

        _ticketStoreMock.Setup(s => s.ConsumeTicket("valid-ticket")).Returns(dashboardPrincipal);

        var result = await _controller.LoginToDashboard("valid-ticket") as RedirectResult;

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Url, Is.EqualTo("/quartz"));
        _authServiceMock.Verify(a => a.SignInAsync(
            _controller.HttpContext,
            ApiConstants.QuartzDashboardCookieScheme,
            dashboardPrincipal,
            It.Is<AuthenticationProperties>(p => !p.IsPersistent && p.ExpiresUtc.HasValue)), Times.Once);
    }

    [Test]
    public async Task LoginToDashboard_WhenTicketInvalid_ShouldReturnUnauthorized()
    {
        _ticketStoreMock.Setup(s => s.ConsumeTicket("bad-ticket")).Returns((ClaimsPrincipal)null);

        var result = await _controller.LoginToDashboard("bad-ticket");

        Assert.That(result, Is.TypeOf<UnauthorizedResult>());
        _authServiceMock.Verify(a => a.SignInAsync(
            It.IsAny<HttpContext>(),
            It.IsAny<string>(),
            It.IsAny<ClaimsPrincipal>(),
            It.IsAny<AuthenticationProperties>()), Times.Never);
    }
}
