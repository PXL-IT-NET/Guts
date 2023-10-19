using Guts.Client.Core.Utility;
using Moq;
using NUnit.Framework;

namespace Guts.Client.Core.Tests.Utility
{
    [TestFixture]
    public class AuthorizationHandlerTests
    {
        [Test]
        [Ignore("This test opens a browser window")]
        public void CheckIfTokenCanBeRetrievedFromBrowserLoginPage()
        {

            var loginWindowFactoryMock = new Mock<ILoginWindowFactory>();

            var apiBaseUrl = "https://localhost:44318/";
            var webAppBaseUrl = "https://localhost:44376/";

            //var apiBaseUrl = "http://guts-api.pxl.be/";
            //var webAppBaseUrl = "http://guts-web.pxl.be/";

            loginWindowFactoryMock.Setup(factory => factory.Create()).Returns(
                () =>
                {
                    var httpHandler = new HttpClientToHttpHandlerAdapter(apiBaseUrl);
                    return new LoginWindow(httpHandler, webAppBaseUrl);
                });

            var authorizationHandler = new AuthorizationHandler(loginWindowFactoryMock.Object);

            var retrieveTokenTask = authorizationHandler.RetrieveRemoteAccessTokenAsync();

            var retrievedToken = retrieveTokenTask.Result;

            Assert.That(retrievedToken, Is.Not.Empty);
        }
    }
}
