namespace Guts.Client.Core.Utility;

public interface IAuthorizationHandler
{
    string RetrieveLocalAccessToken();
    Task<string> RetrieveRemoteAccessTokenAsync();
}