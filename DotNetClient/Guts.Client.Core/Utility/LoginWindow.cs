using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Guts.Client.Core.Utility;

public class LoginWindow(IHttpHandler httpHandler, string webAppBaseUrl) : ILoginWindow
{
    public event TokenRetrievedHandler? TokenRetrieved;
    public event EventHandler? Closed;

    public async Task StartLoginProcedureAsync()
    {
        if (TokenRetrieved == null)
        {
            throw new FieldAccessException($"No handler set for {nameof(TokenRetrieved)} event.");
        }

        if (Closed == null)
        {
            throw new FieldAccessException($"No handler set for {nameof(Closed)} event.");
        }

        var session = await CreateLoginSessionAsync();

        //open login window
        Uri webAppBaseUri = new Uri(webAppBaseUrl);
        Uri loginPageUri = new Uri(webAppBaseUri, $"login?s={session.PublicIdentifier}");
        OpenUrlInBrowser(loginPageUri.AbsoluteUri);

        var maxLoginTimeInSeconds = 90;
        var pollingIntervalInMilliSeconds = 400;
        var token = await WaitForTokenAsync(session.PublicIdentifier, session.SessionToken, pollingIntervalInMilliSeconds, maxLoginTimeInSeconds);

        if (!string.IsNullOrEmpty(token))
        {
            TokenRetrieved?.Invoke(token);
        }
    }

    private async Task<LoginSession> CreateLoginSessionAsync()
    {
        return await httpHandler.PostAsJsonAsync<object, LoginSession>("api/auth/loginsession", new object());
    }

    private void OpenUrlInBrowser(string url)
    {
        try
        {
            Process.Start(url);
        }
        catch
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                url = url.Replace("&", "^&");
                Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Process.Start("xdg-open", url);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start("open", url);
            }
            else
            {
                throw;
            }
        }
    }

    private async Task<string> WaitForTokenAsync(string loginSessionPublicIdentifier, string sessionToken, int delayInMilliSeconds, int timeoutInSeconds)
    {
        var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var pollingTask = Task.Run(async () =>
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            while (!cancellationToken.IsCancellationRequested && stopWatch.Elapsed.TotalSeconds < timeoutInSeconds)
            {
                LoginSession session =
                    await httpHandler.PostAsJsonAsync<string, LoginSession>(
                        $"api/auth/loginsession/{loginSessionPublicIdentifier}", sessionToken);

                if (!string.IsNullOrEmpty(session.LoginToken))
                {
                    return session.LoginToken;
                }

                if (session.IsCancelled)
                {
                    Closed?.Invoke(this, EventArgs.Empty);
                    return string.Empty;
                }

                Thread.Sleep(delayInMilliSeconds);
            }
            stopWatch.Stop();
            Closed?.Invoke(this, EventArgs.Empty);
            return string.Empty;
        }, cancellationToken);

        return await pollingTask;
    }
}