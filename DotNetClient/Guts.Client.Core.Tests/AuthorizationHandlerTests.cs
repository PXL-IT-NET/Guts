using System;
using Guts.Client.Shared.Utility;
using Microsoft.AspNetCore.SignalR.Client;
using Moq;
using NUnit.Framework;

namespace Guts.Client.Core.Tests
{
    [TestFixture]
    public class AuthorizationHandlerTests
    {
        [Test]
        [Ignore("This test opens a browser window")]
        public void CheckIfLoginWindowOpens()
        {
            var usedSessionId = Guid.NewGuid().ToString();
            var sessionIdGeneratorMock = new Mock<ISessionIdGenerator>();
            sessionIdGeneratorMock.Setup(generator => generator.NewId()).Returns(usedSessionId);
            var loginWindowFactoryMock = new Mock<ILoginWindowFactory>();
            loginWindowFactoryMock.Setup(factory => factory.Create()).Returns(() => new LoginWindow(sessionIdGeneratorMock.Object));


            var authorizationHandler = new AuthorizationHandler(loginWindowFactoryMock.Object);

            var expectedToken = Guid.NewGuid().ToString();

            var retrieveTokenTask = authorizationHandler.RetrieveRemoteAccessTokenAsync();

            //Simulate a token being sent from the opened browser
            var gutsHubConnection = new HubConnectionBuilder()
                .WithUrl("https://localhost:44318/authhub")
                .Build();
            gutsHubConnection.StartAsync().Wait();
            gutsHubConnection.SendAsync("SendToken", usedSessionId, expectedToken).Wait();

            var retrievedToken = retrieveTokenTask.Result;

            Assert.That(retrievedToken, Is.EqualTo(expectedToken));
        }
    }
}
