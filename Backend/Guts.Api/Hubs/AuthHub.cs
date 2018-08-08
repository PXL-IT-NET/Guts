using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace Guts.Api.Hubs
{
    public class AuthHub : Hub
    {
        private static readonly ConcurrentDictionary<string, string> _sessionDictionary = new ConcurrentDictionary<string, string>();

        public async Task StartLoginSession(string sessionId)
        {
            if(_sessionDictionary.TryAdd(sessionId, Context.ConnectionId)) //Ensure the session can only be linked to the connection that first starts it
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, sessionId);
            }
        }

        public async Task SendToken(string sessionId, string token)
        {
            await Clients.Group(sessionId).SendAsync("ReceiveToken", token);
        }

        public async Task Cancel(string sessionId)
        {
            await Clients.Group(sessionId).SendAsync("Cancel");
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var sessionKeyValuePair = _sessionDictionary.FirstOrDefault(kv => kv.Value == Context.ConnectionId);
            if (!sessionKeyValuePair.Equals(default(KeyValuePair<string, string>)))
            {
                _sessionDictionary.TryRemove(sessionKeyValuePair.Key, out var _);
            }
            await base.OnDisconnectedAsync(exception);
        }
    }
}
