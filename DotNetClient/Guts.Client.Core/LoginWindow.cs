using Guts.Client.Shared.Utility;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;


namespace Guts.Client.Core
{
    public interface ISessionIdGenerator
    {
        string NewId();
    }

    public class GuidSessionIdGenerator : ISessionIdGenerator
    {
        public string NewId()
        {
            return Guid.NewGuid().ToString();
        }
    }

    public class LoginWindow : ILoginWindow
    {
        private readonly ISessionIdGenerator _sessionIdGenerator;
        private readonly string _apiBaseUrl;
        private readonly string _webAppBaseUrl;
        private HubConnection _gutsHubConnection;

        public event TokenRetrievedHandler TokenRetrieved;
        public event EventHandler Closed;

        public LoginWindow(ISessionIdGenerator sessionIdGenerator, string apiBaseUrl, string webAppBaseUrl)
        {
            _sessionIdGenerator = sessionIdGenerator;
            _apiBaseUrl = apiBaseUrl;
            _webAppBaseUrl = webAppBaseUrl;
        }

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

            var sessionId = _sessionIdGenerator.NewId();

            //connect to the hub
            Uri apiBaseUri = new Uri(_apiBaseUrl);
            Uri hubUri = new Uri(apiBaseUri, "authhub");
            _gutsHubConnection = new HubConnectionBuilder()
                .WithUrl(hubUri.AbsoluteUri)
                .ConfigureLogging(logging =>
                {
                    logging.SetMinimumLevel(LogLevel.Information);
                    logging.AddDebug();
                })
                .Build();

            _gutsHubConnection.On<string>("ReceiveToken", token =>
            {
                TokenRetrieved.Invoke(token);
            });

            _gutsHubConnection.On("Cancel", () =>
            {
                Closed.Invoke(this, new EventArgs());
            });

            await _gutsHubConnection.StartAsync();

            await _gutsHubConnection.SendAsync("StartLoginSession", sessionId);

            //open login window
            Uri webAppBaseUri = new Uri(_webAppBaseUrl);
            Uri loginPageUri = new Uri(webAppBaseUri, $"login?s={sessionId}");
            OpenUrlInBrowser(loginPageUri.AbsoluteUri);
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
    }
}