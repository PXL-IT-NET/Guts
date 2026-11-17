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
using NUnit.Framework;

namespace Guts.Api.Tests.Filters;

[TestFixture]
public class ExamGuestLockdownFilterTests
{
    private ExamGuestLockdownFilter _filter;

    [SetUp]
    public void Setup()
    {
        _filter = new ExamGuestLockdownFilter();
    }

    [Test]
    public async Task OnAuthorizationAsync_RegularUserWithoutGuestClaim_AllowsThrough()
    {
        var context = CreateContext(isExamGuest: false, examIdClaim: null, routeExamId: null, hasAllowExamGuest: false);

        await _filter.OnAuthorizationAsync(context);

        Assert.That(context.Result, Is.Null);
    }

    [Test]
    public async Task OnAuthorizationAsync_ExamGuestCallingNonExamEndpoint_ReturnsForbid()
    {
        var context = CreateContext(isExamGuest: true, examIdClaim: "42", routeExamId: "42", hasAllowExamGuest: false);

        await _filter.OnAuthorizationAsync(context);

        Assert.That(context.Result, Is.InstanceOf<ForbidResult>());
    }

    [Test]
    public async Task OnAuthorizationAsync_ExamGuestWithMismatchedExamId_ReturnsForbid()
    {
        var context = CreateContext(isExamGuest: true, examIdClaim: "42", routeExamId: "99", hasAllowExamGuest: true);

        await _filter.OnAuthorizationAsync(context);

        Assert.That(context.Result, Is.InstanceOf<ForbidResult>());
    }

    [Test]
    public async Task OnAuthorizationAsync_ExamGuestWithMissingClaimOrMissingRouteExamId_ReturnsForbid()
    {
        var missingClaim = CreateContext(isExamGuest: true, examIdClaim: null, routeExamId: "42", hasAllowExamGuest: true);
        await _filter.OnAuthorizationAsync(missingClaim);
        Assert.That(missingClaim.Result, Is.InstanceOf<ForbidResult>());

        var missingRoute = CreateContext(isExamGuest: true, examIdClaim: "42", routeExamId: null, hasAllowExamGuest: true);
        await _filter.OnAuthorizationAsync(missingRoute);
        Assert.That(missingRoute.Result, Is.InstanceOf<ForbidResult>());
    }

    [Test]
    public async Task OnAuthorizationAsync_ExamGuestCallingAllowedEndpointWithMatchingExamId_AllowsThrough()
    {
        var context = CreateContext(isExamGuest: true, examIdClaim: "42", routeExamId: "42", hasAllowExamGuest: true);

        await _filter.OnAuthorizationAsync(context);

        Assert.That(context.Result, Is.Null);
    }

    private static AuthorizationFilterContext CreateContext(
        bool isExamGuest,
        string examIdClaim,
        string routeExamId,
        bool hasAllowExamGuest)
    {
        var httpContext = new DefaultHttpContext();
        var claims = new List<Claim>();
        if (isExamGuest)
        {
            claims.Add(new Claim(GutsClaimTypes.ExamGuest, "true"));
        }
        if (examIdClaim != null)
        {
            claims.Add(new Claim(GutsClaimTypes.ExamId, examIdClaim));
        }

        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));

        var routeData = new RouteData();
        if (routeExamId != null)
        {
            routeData.Values["examId"] = routeExamId;
        }

        var descriptor = new ActionDescriptor
        {
            EndpointMetadata = hasAllowExamGuest ? [new AllowExamGuestAttribute()] : []
        };

        var actionContext = new ActionContext(httpContext, routeData, descriptor);
        return new AuthorizationFilterContext(actionContext, []);
    }
}
