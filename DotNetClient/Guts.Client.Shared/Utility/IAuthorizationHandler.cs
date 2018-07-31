using System.Threading.Tasks;

namespace Guts.Client.Shared.Utility
{
    public interface IAuthorizationHandler
    {
        string RetrieveLocalAccessToken();
        Task<string> RetrieveRemoteAccessTokenAsync();
    }
}