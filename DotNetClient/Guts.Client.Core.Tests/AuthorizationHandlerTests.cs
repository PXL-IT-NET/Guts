using Guts.Client.Shared.Utility;
using Moq;
using NUnit.Framework;
using System;

namespace Guts.Client.Core.Tests
{
    [TestFixture]
    public class AuthorizationHandlerTests
    {
        [Test]
        [Ignore("This test opens a browser window")]
        public void CheckIfTokenCanBeRetrievedFromBrowserLoginPage()
        {
            var usedSessionId = Guid.NewGuid().ToString();
            var sessionIdGeneratorMock = new Mock<ISessionIdGenerator>();
            sessionIdGeneratorMock.Setup(generator => generator.NewId()).Returns(usedSessionId);
            var loginWindowFactoryMock = new Mock<ILoginWindowFactory>();

            //loginWindowFactoryMock.Setup(factory => factory.Create()).Returns(
            //    () => new LoginWindow(sessionIdGeneratorMock.Object,
            //        "https://localhost:44318/",
            //        "https://localhost:44376/"));

            loginWindowFactoryMock.Setup(factory => factory.Create()).Returns(
                () => new LoginWindow(sessionIdGeneratorMock.Object,
                    "http://guts-api.appspot.com/",
                    "http://guts-web.appspot.com/"));

            var authorizationHandler = new AuthorizationHandler(loginWindowFactoryMock.Object);

            var retrieveTokenTask = authorizationHandler.RetrieveRemoteAccessTokenAsync();

            var retrievedToken = retrieveTokenTask.Result;

            Assert.That(retrievedToken, Is.Not.Empty);
        }
    }
}
