using System;
using System.Collections.Generic;
using System.Net;
using Guts.Api.Filters;
using Guts.Api.Models;
using Guts.Business;
using Guts.Common;
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
public class ApplicationExceptionsFilterAttributeTests
{
    private Mock<ILogger<ApplicationExceptionFilterAttribute>> _loggerMock;
    private ApplicationExceptionFilterAttribute _filter;

    [SetUp]
    public void Setup()
    {
        _loggerMock = new Mock<ILogger<ApplicationExceptionFilterAttribute>>();
        _filter = new ApplicationExceptionFilterAttribute(_loggerMock.Object);
    }

    [Test]
    public void OnException_ContractException_ShouldSetBadRequestStatusCodeAndErrorModel()
    {
        // Arrange
        var exception = new ContractException("Contract violation message");
        ExceptionContext context = CreateExceptionContext(exception);

        // Act
        _filter.OnException(context);

        // Assert
        Assert.That(context.HttpContext.Response.StatusCode, Is.EqualTo((int)HttpStatusCode.BadRequest));
        var jsonResult = context.Result as JsonResult;
        Assert.That(jsonResult, Is.Not.Null);
        var errorModel = jsonResult.Value as ErrorModel;
        Assert.That(errorModel, Is.Not.Null);
        Assert.That(errorModel.Message, Is.EqualTo(exception.Message));
    }

    [Test]
    public void OnException_InvalidOperationException_ShouldSetBadRequestStatusCodeAndErrorModel()
    {
        // Arrange
        var exception = new InvalidOperationException("Invalid operation message");
        ExceptionContext context = CreateExceptionContext(exception);

        // Act
        _filter.OnException(context);

        // Assert
        Assert.That(context.HttpContext.Response.StatusCode, Is.EqualTo((int)HttpStatusCode.BadRequest));
        var jsonResult = context.Result as JsonResult;
        Assert.That(jsonResult, Is.Not.Null);
        var errorModel = jsonResult.Value as ErrorModel;
        Assert.That(errorModel, Is.Not.Null);
        Assert.That(errorModel.Message, Is.EqualTo(exception.Message));
    }

    [Test]
    public void OnException_DataNotFoundException_ShouldSetNotFoundStatusCodeAndErrorModel()
    {
        // Arrange
        var exception = new DataNotFoundException("Resource not found");
        ExceptionContext context = CreateExceptionContext(exception);

        // Act
        _filter.OnException(context);

        // Assert
        Assert.That(context.HttpContext.Response.StatusCode, Is.EqualTo((int)HttpStatusCode.NotFound));
        var jsonResult = context.Result as JsonResult;
        Assert.That(jsonResult, Is.Not.Null);
        var errorModel = jsonResult.Value as ErrorModel;
        Assert.That(errorModel, Is.Not.Null);
        Assert.That(errorModel.Message, Is.EqualTo(exception.Message));
    }

    [Test]
    public void OnException_UnhandledException_ShouldLogErrorAndSetInternalServerErrorStatusCode()
    {
        // Arrange
        var exception = new Exception("Unexpected error details");
        ExceptionContext context = CreateExceptionContext(exception);

        // Act
        _filter.OnException(context);

        // Assert
        Assert.That(context.HttpContext.Response.StatusCode, Is.EqualTo((int)HttpStatusCode.InternalServerError));
        var jsonResult = context.Result as JsonResult;
        Assert.That(jsonResult, Is.Not.Null);
        var errorModel = jsonResult.Value as ErrorModel;
        Assert.That(errorModel, Is.Not.Null);
        Assert.That(errorModel.Message, Is.EqualTo("An unexpected error has occurred."));

        _loggerMock.Verify(
            logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    private static ExceptionContext CreateExceptionContext(Exception exception)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = "GET";
        httpContext.Request.Scheme = "https";
        httpContext.Request.Host = new HostString("localhost");
        httpContext.Request.Path = "/api/test";

        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        return new ExceptionContext(actionContext, new List<IFilterMetadata>())
        {
            Exception = exception
        };
    }
}
