using System;
using Guts.Common.Extensions;
using Guts.Domain.UserAggregate;

namespace Guts.Business.Tests.Builders
{
    public class UserBuilder
    {
        private readonly User _user;

        public UserBuilder()
        {
            var random = new Random();
            _user = new User
            {
                Id = random.NextPositive(),
                FirstName = Guid.NewGuid().ToString(),
                LastName = Guid.NewGuid().ToString()
            };
        }

        public UserBuilder WithId(int id)
        {
            _user.Id = id;
            return this;
        }

        public User Build()
        {
            return _user;
        }
    }
}