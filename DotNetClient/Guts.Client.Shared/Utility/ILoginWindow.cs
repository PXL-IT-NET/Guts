using System;
using System.Threading.Tasks;

namespace Guts.Client.Shared.Utility
{
    public delegate Task<LoginResult> CredentialsProvidedHandler(string username, string password);

    public interface ILoginWindow
    {
        event CredentialsProvidedHandler CredentialsProvided;
        event EventHandler Closed;

        void Start();
    }

    public interface ILoginWindowFactory
    {
        ILoginWindow Create();
    }
}