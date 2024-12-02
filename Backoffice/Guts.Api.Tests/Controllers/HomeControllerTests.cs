using Guts.Api.Controllers;
using Guts.Domain.UserAggregate;
using Microsoft.AspNetCore.Mvc;
using NUnit.Framework;

namespace Guts.Api.Tests.Controllers;

public class HomeControllerTests
{
    private HomeController _controller;

    [SetUp]
    public void Setup()
    {
        _controller = new HomeController();
    }

    [Test]
    public void Index_ShouldPermanentlyRedirectToSwaggerUi()
    {
        // Act
        var result = _controller.Index() as RedirectResult;

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Permanent);
        Assert.AreEqual("~/swagger", result.Url);
    }
}