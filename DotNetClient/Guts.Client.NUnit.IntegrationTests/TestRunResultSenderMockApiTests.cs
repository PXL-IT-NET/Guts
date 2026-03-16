using Guts.Client.Core.Models;
using Guts.Client.Core.Utility;
using Guts.Client.NUnit.IntegrationTests.Infrastructure;
using NUnit.Framework;

namespace Guts.Client.NUnit.IntegrationTests;

public class TestRunResultSenderMockApiTests
{
    [Test]
    public async Task SendAsync_ShouldPostAssignmentTestRun_ToMockApi()
    {
        using var mockApiServer = MockGutsApiServer.Start();

        var httpHandler = new HttpClientToHttpHandlerAdapter(mockApiServer.BaseUrl);
        var authorizationHandler = new FakeAuthorizationHandler("dummy-token");
        var outputWriter = new FakeOutputWriter();
        var sender = new TestRunResultSender(httpHandler, authorizationHandler, outputWriter);

        var assignment = new Assignment
        {
            CourseCode = "dummyCourse",
            TopicCode = "dummyChapter",
            AssignmentCode = "dummyExercise"
        };

        var testRun = new AssignmentTestRun(
            assignment,
            [new TestResult("A test", true, string.Empty)],
            [],
            "dummy-hash");

        var result = await sender.SendAsync(testRun, TestRunType.ForExercise);

        Assert.That(result.Success, Is.True);

        var sentRequest = mockApiServer.CapturedRequests
            .FirstOrDefault(r => r.Method == "POST" && r.Path.Equals("/api/testruns/forexercise", StringComparison.OrdinalIgnoreCase));

        Assert.That(sentRequest, Is.Not.Null);
        Assert.That(sentRequest!.AuthorizationHeader, Is.EqualTo("Bearer dummy-token"));
        Assert.That(sentRequest.Body, Does.Contain("dummyCourse"));
    }

    private class FakeAuthorizationHandler(string token) : IAuthorizationHandler
    {
        public string RetrieveLocalAccessToken() => token;

        public Task<string> RetrieveRemoteAccessTokenAsync()
        {
            return Task.FromResult(token);
        }
    }

    private class FakeOutputWriter : ITestOutputWriter
    {
        public void WriteError(string error)
        {
        }

        public void WriteError(Exception exception)
        {
        }

        public void WriteProgress(string message)
        {
        }
    }
}
