using System;
using System.Threading.Tasks;
using Guts.Api.Controllers;
using Guts.Business.Repositories;
using Guts.Business.Tests.Builders;
using Guts.Common.Extensions;
using Guts.Domain.TestAggregate;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;

namespace Guts.Api.Tests.Controllers;

public class TestControllerTests
{
    private TestController _controller;
    private Mock<ITestRepository> _testRepositoryMock = null!;

    [SetUp]
    public void Setup()
    {
        _testRepositoryMock = new Mock<ITestRepository>();
        _controller = new TestController(_testRepositoryMock.Object);
    }

    [Test]
    public async Task Delete_TestExists_ShouldUseRepoToDeleteTest_ShouldReturnOk()
    {
        // Arrange
        Test testToDelete = new TestBuilder().WithId().Build();
        _testRepositoryMock.Setup(r => r.GetByIdAsync(testToDelete.Id)).ReturnsAsync(testToDelete);

        // Act
        var result = (await _controller.Delete(testToDelete.Id)) as OkResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        _testRepositoryMock.Verify(r => r.DeleteAsync(testToDelete), Times.Once);
    }

    [Test]
    public async Task Delete_TestDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        int testId = Random.Shared.NextPositive();
        _testRepositoryMock.Setup(r => r.GetByIdAsync(testId)).ReturnsAsync(() => null);

        // Act
        var result = (await _controller.Delete(testId)) as NotFoundResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        _testRepositoryMock.Verify(r => r.GetByIdAsync(testId), Times.Once);
    }
}