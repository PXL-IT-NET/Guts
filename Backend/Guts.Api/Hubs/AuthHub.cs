using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;

namespace Guts.Api.Hubs
{
    public class SignalROptions
    {
        public string SessionSalt { get; set; }
    }

    public class AuthHub : Hub
    {
        private readonly IOptions<SignalROptions> _options;

        public AuthHub(IOptions<SignalROptions> options)
        {
            _options = options;
        }

        public async Task StartLoginSession(string sessionId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, GetGroupNameForSession(sessionId));
        }

        public async Task SendToken(string sessionId, string token)
        {
            await Clients.Group(GetGroupNameForSession(sessionId)).SendAsync("ReceiveToken", token);
        }

        public async Task Cancel(string sessionId)
        {
            await Clients.Group(GetGroupNameForSession(sessionId)).SendAsync("Cancel");
        }

        private string GetGroupNameForSession(string sessionId)
        {
            return sessionId + _options.Value.SessionSalt;
        }
    }
}
