using System;
using Guts.Common.Extensions;
using Guts.Domain.UserAggregate;

namespace Guts.Domain.Tests.Builders
{
    public class UserBuilder
    {
        private readonly User _user;
        private readonly Random _random;

        public UserBuilder() : this(new Random())
        {
        }

        public UserBuilder(Random random)
        {
            _random = random;
            _user = new User
            {
                Id = _random.NextPositive(),
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
            _user.Id = _random.NextPositive();
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