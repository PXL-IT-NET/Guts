using Guts.Client.Shared.Utility;

namespace Guts.Client.Core
{
    public class LoginWindowFactory : ILoginWindowFactory
    {
        private readonly string _apiBaseUrl;
        private readonly string _webAppBaseUrl;

        public LoginWindowFactory(string apiBaseUrl, string webAppBaseUrl)
        {
            _apiBaseUrl = apiBaseUrl;
            _webAppBaseUrl = webAppBaseUrl;
        }

        public ILoginWindow Create()
        {
            return new LoginWindow(new GuidSessionIdGenerator(), _apiBaseUrl, _webAppBaseUrl);
        }
    }
}