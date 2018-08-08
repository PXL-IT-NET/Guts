using Guts.Client.Shared.Utility;

namespace Guts.Client.Classic.UI
{
    public class LoginWindowFactory : ILoginWindowFactory
    {
        private readonly IHttpHandler _httpHandler;

        public LoginWindowFactory(IHttpHandler httpHandler)
        {
            _httpHandler = httpHandler;
        }

        public ILoginWindow Create()
        {
            return new LoginWindow(_httpHandler);
        }
    }
}