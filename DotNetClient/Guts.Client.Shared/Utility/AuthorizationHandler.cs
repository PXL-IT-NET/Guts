using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

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
                        retrieveTokenTaskCompletionSource.TrySetResult(token);
                    };

                    loginWindow.Closed += (sender, e) =>
                    {
                        retrieveTokenTaskCompletionSource.TrySetCanceled();
                    };

                    TestContext.Progress.WriteLine("Opening login window...");
                    await loginWindow.StartLoginProcedureAsync();
                    TestContext.Progress.WriteLine("Login window open.");
                }
                catch (Exception ex)
                {
                    retrieveTokenTaskCompletionSource.TrySetException(ex);
                }
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();

            var maxLoginTimeInSeconds = 90;
            TestContext.Progress.WriteLine($"Waiting {maxLoginTimeInSeconds} seconds (at most) for the user to login...");
            if (await Task.WhenAny(retrieveTokenTaskCompletionSource.Task, Task.Delay(maxLoginTimeInSeconds * 1000)) != retrieveTokenTaskCompletionSource.Task)
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