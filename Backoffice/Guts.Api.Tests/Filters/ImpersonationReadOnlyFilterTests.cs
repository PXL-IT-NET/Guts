using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Guts.Api.Filters;
using Guts.Application.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Moq;
using NUnit.Framework;

namespace Guts.Api.Tests.Filters;

[TestFixture]
public class ImpersonationReadOnlyFilterTests
{
    private ImpersonationReadOnlyFilter _filter;

    [SetUp]
    public void SetUp()
    {
        _filter = new ImpersonationReadOnlyFilter();
    }

    [TestCase("POST")]
    [TestCase("PUT")]
    [TestCase("PATCH")]
    [TestCase("DELETE")]
    public async Task OnActionExecutionAsync_WhenImpersonatingAndMutating_Returns403Forbidden(string method)
    {
        var context = CreateContext(method, isImpersonating: true);
        bool nextCalled = false;

        await _filter.OnActionExecutionAsync(context, () =>
        {
            nextCalled = true;
            return Task.FromResult(new ActionExecutedContext(context, [], new Mock<Controller>().Object));
        });

        Assert.That(nextCalled, Is.False);
        var result = context.Result as ObjectResult;
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(StatusCodes.Status403Forbidden));
    }

    [Test]
    public async Task OnActionExecutionAsync_WhenImpersonatingAndGet_CallsNext()
    {
        var context = CreateContext("GET", isImpersonating: true);
        bool nextCalled = false;

        await _filter.OnActionExecutionAsync(context, () =>
        {
            nextCalled = true;
            return Task.FromResult(new ActionExecutedContext(context, [], new Mock<Controller>().Object));
        });

        Assert.That(nextCalled, Is.True);
        Assert.That(context.Result, Is.Null);
    }

    [Test]
    public async Task OnActionExecutionAsync_WhenNotImpersonatingAndPost_CallsNext()
    {
        var context = CreateContext("POST", isImpersonating: false);
        bool nextCalled = false;

        await _filter.OnActionExecutionAsync(context, () =>
        {
            nextCalled = true;
            return Task.FromResult(new ActionExecutedContext(context, [], new Mock<Controller>().Object));
        });

        Assert.That(nextCalled, Is.True);
        Assert.That(context.Result, Is.Null);
    }

    private static ActionExecutingContext CreateContext(string method, bool isImpersonating)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = method;
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, "5") };
        if (isImpersonating)
        {
            claims.Add(new Claim(GutsClaimTypes.ImpersonatorId, "1"));
        }
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));

        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        return new ActionExecutingContext(actionContext, [], new Dictionary<string, object>(), new Mock<Controller>().Object);
    }
}
