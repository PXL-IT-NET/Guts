using System;
using System.Threading.Tasks;

namespace Guts.Client.Shared.Utility
{
    public delegate void TokenRetrievedHandler(string token);

    public interface ILoginWindow
    {
        event TokenRetrievedHandler TokenRetrieved;
        event EventHandler Closed;

        Task StartLoginProcedureAsync();
    }

    public interface ILoginWindowFactory
    {
        ILoginWindow Create();
    }
}