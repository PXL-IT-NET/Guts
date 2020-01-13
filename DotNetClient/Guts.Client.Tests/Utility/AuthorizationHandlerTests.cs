using System.Threading;
using Guts.Client.Classic.UI;
using Guts.Client.Shared.Utility;
using Moq;
using NUnit.Framework;
using LoginWindowFactory = Guts.Client.Classic.UI.LoginWindowFactory;

namespace Guts.Client.Tests.Utility
{
    [TestFixture, Apartment(ApartmentState.STA)]
    public class AuthorizationHandlerTests
    {
        [Test]
        [Ignore("This test opens a WPF window and can only end if the window is closed manually")]
        public void CheckIfLoginWindowOpens()
        {
            var httpHandlerMock = new Mock<IHttpHandler>();
            var loginWindowFactory = new LoginWindowFactory(httpHandlerMock.Object);
            var authorizationHandler = new AuthorizationHandler(loginWindowFactory);

            var token = authorizationHandler.RetrieveRemoteAccessTokenAsync().Result;
        }
    }
}
