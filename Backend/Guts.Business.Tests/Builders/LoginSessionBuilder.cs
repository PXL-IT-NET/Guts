using System;
using Guts.Domain.LoginSessionAggregate;

namespace Guts.Business.Tests.Builders
{
    public class LoginSessionBuilder
    {
        private readonly Random _random;
        private readonly LoginSession _session;

        public LoginSessionBuilder()
        {
            _random = new Random();
            _session = new LoginSession
            {
                CreateDateTime = DateTime.UtcNow.AddMinutes(-1 * _random.Next(60 * 10)),
                IpAddress = Guid.NewGuid().ToString(),
                IsCancelled = false,
                LoginToken = null,
                PublicIdentifier = Guid.NewGuid().ToString(),
                SessionToken = Guid.NewGuid().ToString()
            };
        }

        public LoginSessionBuilder WithIsCancelled(bool isCancelled)
        {
            _session.IsCancelled = isCancelled;
            return this;
        }

        public LoginSessionBuilder WithCreationDateTime(DateTime dateTime)
        {
            _session.CreateDateTime = dateTime;
            return this;
        }

        public LoginSession Build()
        {
            return _session;
        }

        
    }
}