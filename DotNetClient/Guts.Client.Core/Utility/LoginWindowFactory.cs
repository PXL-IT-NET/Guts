namespace Guts.Client.Core.Utility;

public class LoginWindowFactory(IHttpHandler httpHandler, string webAppBaseUrl) : ILoginWindowFactory
{
    public ILoginWindow Create()
    {
        return new LoginWindow(httpHandler, webAppBaseUrl);
    }
}