using System;
using Guts.Common.Extensions;
using Guts.Domain.UserAggregate;

namespace Guts.Domain.Tests.Builders
{
    public class UserBuilder
    {
        private readonly User _user;

        public UserBuilder()
        {
            _user = new User
            {
                Id = Random.Shared.NextPositive(),
                FirstName = Guid.NewGuid().ToString(),
                LastName = Guid.NewGuid().ToString()
            };
        }

        public UserBuilder WithId(int id)
        {
            _user.Id = id;
            return this;
        }

        public UserBuilder WithId()
        {
            _user.Id = Random.Shared.NextPositive();
            return this;
        }

        public UserBuilder WithFirstName(string firstName)
        {
            _user.FirstName = firstName;
            return this;
        }

        public UserBuilder WithLastName(string lastName)
        {
            _user.LastName = lastName;
            return this;
        }

        public User Build()
        {
            return _user;
        }

      
    }
}