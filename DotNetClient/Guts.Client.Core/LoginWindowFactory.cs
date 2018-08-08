using Guts.Client.Shared.Utility;

namespace Guts.Client.Core
{
    public class LoginWindowFactory : ILoginWindowFactory
    {
        public ILoginWindow Create()
        {
            return new LoginWindow(new GuidSessionIdGenerator());
        }
    }
}