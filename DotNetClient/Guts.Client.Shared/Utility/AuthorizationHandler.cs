using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Guts.Client.Shared.Utility
{
    public class AuthorizationHandler : IAuthorizationHandler
    {
        private readonly ILoginWindowFactory _loginWindowFactory;

        public AuthorizationHandler(ILoginWindowFactory loginWindowFactory)
        {
            _loginWindowFactory = loginWindowFactory;
        }

        public string RetrieveLocalAccessToken()
        {
            return !File.Exists(LocalTokenFilePath) ? string.Empty : File.ReadAllText(LocalTokenFilePath);
        }

        public async Task<string> RetrieveRemoteAccessTokenAsync()
        {
            var retrieveTokenTaskCompletionSource = new TaskCompletionSource<string>();

            Thread thread = new Thread(async () =>
            {
                try
                {
                    var loginWindow = _loginWindowFactory.Create();

                    loginWindow.TokenRetrieved += token =>
                    {
                        StoreTokenLocally(token);
                        retrieveTokenTaskCompletionSource.SetResult(token);
                    };

                    loginWindow.Closed += (sender, e) =>
                    {
                        retrieveTokenTaskCompletionSource.SetCanceled();
                    };

                    await loginWindow.StartLoginProcedureAsync();
                }
                catch (Exception ex)
                {
                    retrieveTokenTaskCompletionSource.SetException(ex);
                }
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();

            var maxLoginTimeInSeconds = 90;
            if(await Task.WhenAny(retrieveTokenTaskCompletionSource.Task, Task.Delay(maxLoginTimeInSeconds * 1000)) != retrieveTokenTaskCompletionSource.Task)
            {
                //timeout
                retrieveTokenTaskCompletionSource.SetException(new Exception($"Login timeout. You must login within {maxLoginTimeInSeconds} seconds."));
            }

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