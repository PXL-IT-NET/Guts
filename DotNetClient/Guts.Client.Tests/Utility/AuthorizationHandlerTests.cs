using System.Threading;
using Guts.Client.Classic.UI;
using Guts.Client.Shared.Utility;
using Moq;
using NUnit.Framework;

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
            var loginWindowFactory = new LoginWindowFactory();
            var authorizationHandler = new AuthorizationHandler(httpHandlerMock.Object, loginWindowFactory);

            var token = authorizationHandler.RetrieveRemoteAccessTokenAsync().Result;
        }
    }
}
