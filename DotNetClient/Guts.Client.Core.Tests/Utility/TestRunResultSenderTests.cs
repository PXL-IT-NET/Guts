using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Guts.Client.Core.Models;
using Guts.Client.Core.Utility;
using Moq;
using NUnit.Framework;

namespace Guts.Client.Core.Tests.Utility
{
    [TestFixture]
    public class TestRunResultSenderTests
    {
        private Mock<IHttpHandler> _httpHandlerMock = null!;
        private Mock<IAuthorizationHandler> _authorizationHandlerMock = null!;
        private TestRunResultSender _testRunResultSender = null!;
        private AssignmentTestRun _testRun = null!;

        [SetUp]
        public void SetUp()
        {
            _httpHandlerMock = new Mock<IHttpHandler>();
            _authorizationHandlerMock = new Mock<IAuthorizationHandler>();
            _testRunResultSender = new TestRunResultSender(_httpHandlerMock.Object, _authorizationHandlerMock.Object);

            var assignment = new Assignment
            {
                CourseCode = "TestCourse",
                TopicCode = "TestTopic",
                AssignmentCode = "TestAssignment"
            };

            var testResults = new List<TestResult>
            {
                new TestResult("Test1", true, "Success"),
                new TestResult("Test2", false, "Failure")
            };

            var solutionFiles = new List<SolutionFile>
            {
                new SolutionFile("Test.cs", "public class Test { }")
            };

            _testRun = new AssignmentTestRun(assignment, testResults, solutionFiles, "testhash");
        }

        [TestCase(TestRunType.ForExercise, "api/testruns/forexercise")]
        [TestCase(TestRunType.ForProject, "api/testruns/forproject")]
        public async Task SendAsync_ShouldUseCorrectApiUrl(TestRunType type, string expectedApiUrl)
        {
            // Arrange
            var successResponse = CreateSuccessHttpResponse();
            _authorizationHandlerMock.Setup(x => x.RetrieveLocalAccessToken()).Returns("cached_token");
            _httpHandlerMock.Setup(x => x.PostAsJsonAsync(expectedApiUrl, _testRun))
                           .ReturnsAsync(successResponse);

            // Act
            var result = await _testRunResultSender.SendAsync(_testRun, type);

            // Assert
            Assert.That(result.Success, Is.True);
            _httpHandlerMock.Verify(x => x.PostAsJsonAsync(expectedApiUrl, _testRun), Times.Once);
            _httpHandlerMock.Verify(x => x.UseBearerToken("cached_token"), Times.Once);
        }

        [Test]
        public async Task SendAsync_WithSuccessfulResponse_ShouldReturnSuccessResult()
        {
            // Arrange
            HttpResponseMessage successResponse = CreateSuccessHttpResponse();
            _authorizationHandlerMock.Setup(x => x.RetrieveLocalAccessToken()).Returns("test_token");
            _httpHandlerMock.Setup(x => x.PostAsJsonAsync(It.IsAny<string>(), _testRun))
                           .ReturnsAsync(successResponse);

            // Act
            var result = await _testRunResultSender.SendAsync(_testRun, TestRunType.ForExercise);

            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(result.Message, Is.Null.Or.Empty);
        }

        [Test]
        public async Task SendAsync_WithFailedResponse_ShouldReturnFailureResultWithMessage()
        {
            // Arrange
            var errorMessage = "Bad Request";
            var failureResponse = CreateFailureHttpResponse(HttpStatusCode.BadRequest, errorMessage);
            _authorizationHandlerMock.Setup(x => x.RetrieveLocalAccessToken()).Returns("test_token");
            _httpHandlerMock.Setup(x => x.PostAsJsonAsync(It.IsAny<string>(), _testRun))
                           .ReturnsAsync(failureResponse);

            // Act
            var result = await _testRunResultSender.SendAsync(_testRun, TestRunType.ForExercise);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Is.EqualTo(errorMessage));
        }

        [Test]
        public async Task SendAsync_WithUnauthorizedResponse_ShouldRetryWithRemoteToken()
        {
            // Arrange
            var unauthorizedResponse = CreateFailureHttpResponse(HttpStatusCode.Unauthorized, "Unauthorized");
            var successResponse = CreateSuccessHttpResponse();
            var remoteToken = "remote_token";

            _authorizationHandlerMock.Setup(x => x.RetrieveLocalAccessToken()).Returns("expired_token");
            _authorizationHandlerMock.Setup(x => x.RetrieveRemoteAccessTokenAsync()).ReturnsAsync(remoteToken);

            _httpHandlerMock.SetupSequence(x => x.PostAsJsonAsync(It.IsAny<string>(), _testRun))
                           .ReturnsAsync(unauthorizedResponse)
                           .ReturnsAsync(successResponse);

            // Act
            var result = await _testRunResultSender.SendAsync(_testRun, TestRunType.ForExercise);

            // Assert
            Assert.That(result.Success, Is.True);
            _httpHandlerMock.Verify(x => x.UseBearerToken("expired_token"), Times.Once);
            _httpHandlerMock.Verify(x => x.UseBearerToken(remoteToken), Times.Once);
            _authorizationHandlerMock.Verify(x => x.RetrieveRemoteAccessTokenAsync(), Times.Once);
            _httpHandlerMock.Verify(x => x.PostAsJsonAsync(It.IsAny<string>(), _testRun), Times.Exactly(2));
        }

        [Test]
        public async Task SendAsync_WithExceptionDuringFirstRequest_ShouldRetryWithRemoteToken()
        {
            // Arrange
            var successResponse = CreateSuccessHttpResponse();
            var remoteToken = "remote_token";

            _authorizationHandlerMock.Setup(x => x.RetrieveLocalAccessToken()).Returns("test_token");
            _authorizationHandlerMock.Setup(x => x.RetrieveRemoteAccessTokenAsync()).ReturnsAsync(remoteToken);

            _httpHandlerMock.SetupSequence(x => x.PostAsJsonAsync(It.IsAny<string>(), _testRun))
                           .ThrowsAsync(new HttpRequestException("Network error"))
                           .ReturnsAsync(successResponse);

            // Act
            var result = await _testRunResultSender.SendAsync(_testRun, TestRunType.ForExercise);

            // Assert
            Assert.That(result.Success, Is.True);
            _httpHandlerMock.Verify(x => x.UseBearerToken("test_token"), Times.Once);
            _httpHandlerMock.Verify(x => x.UseBearerToken(remoteToken), Times.Once);
            _authorizationHandlerMock.Verify(x => x.RetrieveRemoteAccessTokenAsync(), Times.Once);
            _httpHandlerMock.Verify(x => x.PostAsJsonAsync(It.IsAny<string>(), _testRun), Times.Exactly(2));
        }

        [Test]
        public async Task SendAsync_WithCachedTokenAvailable_ShouldUseCachedTokenFirst()
        {
            // Arrange
            var cachedToken = "cached_token";
            var successResponse = CreateSuccessHttpResponse();

            _authorizationHandlerMock.Setup(x => x.RetrieveLocalAccessToken()).Returns(cachedToken);
            _httpHandlerMock.Setup(x => x.PostAsJsonAsync(It.IsAny<string>(), _testRun))
                           .ReturnsAsync(successResponse);

            // Act
            var result = await _testRunResultSender.SendAsync(_testRun, TestRunType.ForExercise);

            // Assert
            Assert.That(result.Success, Is.True);
            _httpHandlerMock.Verify(x => x.UseBearerToken(cachedToken), Times.Once);
            _authorizationHandlerMock.Verify(x => x.RetrieveLocalAccessToken(), Times.Once);
            _authorizationHandlerMock.Verify(x => x.RetrieveRemoteAccessTokenAsync(), Times.Never);
        }

        [Test]
        public async Task SendAsync_WithNoCachedToken_ShouldRetrieveRemoteToken()
        {
            // Arrange
            var remoteToken = "remote_token";
            var successResponse = CreateSuccessHttpResponse();

            _authorizationHandlerMock.Setup(x => x.RetrieveLocalAccessToken()).Returns(string.Empty);
            _authorizationHandlerMock.Setup(x => x.RetrieveRemoteAccessTokenAsync()).ReturnsAsync(remoteToken);
            _httpHandlerMock.Setup(x => x.PostAsJsonAsync(It.IsAny<string>(), _testRun))
                           .ReturnsAsync(successResponse);

            // Act
            var result = await _testRunResultSender.SendAsync(_testRun, TestRunType.ForExercise);

            // Assert
            Assert.That(result.Success, Is.True);
            _httpHandlerMock.Verify(x => x.UseBearerToken(remoteToken), Times.Once);
            _authorizationHandlerMock.Verify(x => x.RetrieveLocalAccessToken(), Times.Once);
            _authorizationHandlerMock.Verify(x => x.RetrieveRemoteAccessTokenAsync(), Times.Once);
        }

        [Test]
        public async Task SendAsync_WhenRetryAlsoFails_ShouldReturnFailureResult()
        {
            // Arrange
            var unauthorizedResponse = CreateFailureHttpResponse(HttpStatusCode.Unauthorized, "Unauthorized");
            var badRequestResponse = CreateFailureHttpResponse(HttpStatusCode.BadRequest, "Bad Request");
            var remoteToken = "remote_token";

            _authorizationHandlerMock.Setup(x => x.RetrieveLocalAccessToken()).Returns("expired_token");
            _authorizationHandlerMock.Setup(x => x.RetrieveRemoteAccessTokenAsync()).ReturnsAsync(remoteToken);

            _httpHandlerMock.SetupSequence(x => x.PostAsJsonAsync(It.IsAny<string>(), _testRun))
                           .ReturnsAsync(unauthorizedResponse)
                           .ReturnsAsync(badRequestResponse);

            // Act
            var result = await _testRunResultSender.SendAsync(_testRun, TestRunType.ForExercise);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Is.EqualTo("Bad Request"));
            _httpHandlerMock.Verify(x => x.PostAsJsonAsync(It.IsAny<string>(), _testRun), Times.Exactly(2));
        }

        [Test]
        public async Task SendAsync_WithFakeSuccessResponseFromPxlFirewall_ShouldReturnFailureResult_WithFirewallSupportId()
        {
            // Arrange
            string expectedSupportId = "17679479214706385589";
            HttpResponseMessage fakeSuccessResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent($"<html><head><title>Request Rejected</title></head><body>The requested URL was rejected. Please consult with your administrator.<br><br>Your support ID is: {expectedSupportId}<br><br><a href='javascript:history.back();'>[Go Back]</a></body></html>")
            };
            _authorizationHandlerMock.Setup(x => x.RetrieveLocalAccessToken()).Returns("test_token");
            _httpHandlerMock.Setup(x => x.PostAsJsonAsync(It.IsAny<string>(), _testRun))
                .ReturnsAsync(fakeSuccessResponse);

            // Act
            var result = await _testRunResultSender.SendAsync(_testRun, TestRunType.ForExercise);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Does.Contain(expectedSupportId));
        }

        private static HttpResponseMessage CreateSuccessHttpResponse()
        {
            return new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = new StringContent("{json-data}")
            };
        }

        private static HttpResponseMessage CreateFailureHttpResponse(HttpStatusCode statusCode, string content)
        {
            return new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(content)
            };
        }
    }
}