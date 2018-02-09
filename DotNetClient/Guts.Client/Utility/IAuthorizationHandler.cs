using System.Threading.Tasks;

namespace Guts.Client.Utility
{
    public interface IAuthorizationHandler
    {
        string RetrieveLocalAccessToken();
        Task<string> RetrieveRemoteAccessTokenAsync();
    }
}