using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Guts.Client.Shared.Utility;
using Microsoft.AspNetCore.SignalR.Client;


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
        private HubConnection _gutsHubConnection;

        public event TokenRetrievedHandler TokenRetrieved;
        public event EventHandler Closed;

        public LoginWindow(ISessionIdGenerator sessionIdGenerator)
        {
            _sessionIdGenerator = sessionIdGenerator;
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

            //TODO: set up signalR connection with Guts website, let the user login and recieve a notification (containing the token)
            var sessionId = _sessionIdGenerator.NewId();
            var loginUrl = $"https://guts-web.appspot.com/login?s={sessionId}";

            //connect to the hub
            _gutsHubConnection = new HubConnectionBuilder()
                .WithUrl("https://localhost:44318/authhub")
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
            OpenUrlInBrowser(loginUrl); 
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