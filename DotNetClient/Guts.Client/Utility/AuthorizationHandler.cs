using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using System.Windows.Threading;
using Guts.Client.UI;

namespace Guts.Client.Utility
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

        public AuthorizationHandler(IHttpHandler httpHandler)
        {
            _httpHandler = httpHandler;
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
                    var loginWindow = new LoginWindow
                    {
                        WindowStartupLocation = WindowStartupLocation.CenterScreen,
                        Topmost = true
                    };

                    var token = string.Empty;

                    loginWindow.DoLogin += async (string username, string password) =>
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

                            return ApiResult.CreateSuccess();
                        }
                        catch (HttpException httpException)
                        {
                            if (!string.IsNullOrEmpty(httpException.Message))
                            {
                                return ApiResult.CreateError(httpException.Message);
                            }

                            if (httpException.GetHttpCode() == (int) HttpStatusCode.Unauthorized)
                            {
                                return ApiResult.CreateError("Invalid email / password combination.");
                            }

                            return ApiResult.CreateError("Unknown error");
                        }
                        catch (HttpRequestException requestException)
                        {
                            return ApiResult.CreateError(requestException.Message);
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
                        loginWindow.Dispatcher.InvokeShutdown();
                    };

                    loginWindow.Show();
                    loginWindow.Focus();

                    Dispatcher.Run();
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