namespace Guts.Client.Core.Utility;

public class AuthorizationHandler(ILoginWindowFactory loginWindowFactory, ITestOutputWriter outputWriter) : IAuthorizationHandler
{
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
                var loginWindow = loginWindowFactory.Create();

                loginWindow.TokenRetrieved += token =>
                {
                    StoreTokenLocally(token);
                    retrieveTokenTaskCompletionSource.TrySetResult(token);
                };

                loginWindow.Closed += (sender, e) =>
                {
                    retrieveTokenTaskCompletionSource.TrySetCanceled();
                };

                outputWriter.WriteProgress("Opening login window...");
                await loginWindow.StartLoginProcedureAsync();
                outputWriter.WriteProgress("Login window open.");
            }
            catch (Exception ex)
            {
                retrieveTokenTaskCompletionSource.TrySetException(ex);
            }
        });

        if (OperatingSystem.IsWindows())
        {
            thread.SetApartmentState(ApartmentState.STA);
        }
        thread.Start();

        var maxLoginTimeInSeconds = 90;
        outputWriter.WriteProgress($"Waiting {maxLoginTimeInSeconds} seconds (at most) for the user to login...");
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