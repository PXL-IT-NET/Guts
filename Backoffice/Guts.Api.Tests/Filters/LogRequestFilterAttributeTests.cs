using System;
using System.Collections.Generic;
using System.Net;
using Guts.Api.Filters;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Guts.Api.Tests.Filters;

[TestFixture]
public class LogRequestFilterAttributeTests
{
    private Mock<ILogger<LogBadRequestFilterAttribute>> _loggerMock;
    private LogBadRequestFilterAttribute _filter;

    [SetUp]
    public void Setup()
    {
        _loggerMock = new Mock<ILogger<LogBadRequestFilterAttribute>>();
        _filter = new LogBadRequestFilterAttribute(_loggerMock.Object);
    }

    [Test]
    public void OnResultExecuted_WhenStatusCodeIsBadRequest_ShouldLogWarning()
    {
        // Arrange
        var result = new BadRequestObjectResult(new { Error = "Invalid input" });
        ResultExecutedContext context = CreateResultExecutedContext((int)HttpStatusCode.BadRequest, result);

        // Act
        _filter.OnResultExecuted(context);

        // Assert
        _loggerMock.Verify(
            logger => logger.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Bad request detected.") && v.ToString().Contains("Invalid input")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Test]
    public void OnResultExecuted_WhenStatusCodeIsOk_ShouldNotLogWarning()
    {
        // Arrange
        var result = new OkResult();
        ResultExecutedContext context = CreateResultExecutedContext((int)HttpStatusCode.OK, result);

        // Act
        _filter.OnResultExecuted(context);

        // Assert
        _loggerMock.Verify(
            logger => logger.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Never);
    }

    private static ResultExecutedContext CreateResultExecutedContext(int statusCode, IActionResult result)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = "POST";
        httpContext.Request.Scheme = "https";
        httpContext.Request.Host = new HostString("localhost");
        httpContext.Request.Path = "/api/test";
        httpContext.Response.StatusCode = statusCode;

        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        return new ResultExecutedContext(
            actionContext,
            new List<IFilterMetadata>(),
            result,
            new object());
    }
}
