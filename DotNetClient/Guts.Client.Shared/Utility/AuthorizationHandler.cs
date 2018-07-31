using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Guts.Client.Shared.Utility
{
    public class AuthorizationHandler : IAuthorizationHandler
    {
        private class TokenResponse
        {
            public string Token { get; set; }
        }

        private class TokenRequest
        {
            public string Email { get; set; }
            public string Password { get; set; }
        }

        private readonly IHttpHandler _httpHandler;
        private readonly ILoginWindowFactory _loginWindowFactory;

        public AuthorizationHandler(IHttpHandler httpHandler, ILoginWindowFactory loginWindowFactory)
        {
            _httpHandler = httpHandler;
            _loginWindowFactory = loginWindowFactory;
        }

        public string RetrieveLocalAccessToken()
        {
            return !File.Exists(LocalTokenFilePath) ? string.Empty : File.ReadAllText(LocalTokenFilePath);
        }

        public async Task<string> RetrieveRemoteAccessTokenAsync()
        {
            var retrieveTokenTaskCompletionSource = new TaskCompletionSource<string>();
            Thread thread = new Thread(() =>
            {
                try
                {
                    var loginWindow = _loginWindowFactory.Create();

                    var token = string.Empty;

                    loginWindow.CredentialsProvided += async (username, password) =>
                    {
                        var tokenRequest = new TokenRequest
                        {
                            Email = username,
                            Password = password
                        };

                        try
                        {
                            var tokenResponse =
                                await _httpHandler.PostAsJsonAsync<TokenRequest, TokenResponse>("api/auth/token",
                                    tokenRequest);
                            token = tokenResponse.Token;
                            StoreTokenLocally(token);

                            return LoginResult.CreateSuccess();
                        }
                        catch (HttpRequestException requestException)
                        {
                            return LoginResult.CreateError(requestException.Message);
                        }
                        catch (HttpResponseException responseException)
                        {
                            if (!string.IsNullOrEmpty(responseException.Message))
                            {
                                return LoginResult.CreateError(responseException.Message);
                            }

                            if (responseException.ResponseStatusCode == HttpStatusCode.Unauthorized)
                            {
                                return LoginResult.CreateError("Invalid email / password combination.");
                            }

                            return LoginResult.CreateError("Unknown error");
                        }
                        
                    };

                    loginWindow.Closed += (sender, e) =>
                    {
                        if (!string.IsNullOrEmpty(token))
                        {
                            retrieveTokenTaskCompletionSource.SetResult(token);
                        }
                        else
                        {
                            retrieveTokenTaskCompletionSource.SetCanceled();
                        }
                    };

                    loginWindow.Start();
                }
                catch (Exception ex)
                {
                    retrieveTokenTaskCompletionSource.SetException(ex);
                }
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            return await retrieveTokenTaskCompletionSource.Task;
        }

        private string GutsAppDataPath
        {
            get
            {
                var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                return Path.Combine(appDataPath, "Guts");
            }
        }

        private string LocalTokenFilePath => Path.Combine(GutsAppDataPath, "_cache.txt");

        private void StoreTokenLocally(string token)
        {
            if (!Directory.Exists(GutsAppDataPath))
            {
                Directory.CreateDirectory(GutsAppDataPath);
            }

            File.WriteAllText(LocalTokenFilePath, token);
        }
    }
}