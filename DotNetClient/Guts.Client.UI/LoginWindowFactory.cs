using Guts.Client.Shared.Utility;

namespace Guts.Client.Classic.UI
{
    public class LoginWindowFactory : ILoginWindowFactory
    {
        public ILoginWindow Create()
        {
            return new LoginWindow();
        }
    }
}