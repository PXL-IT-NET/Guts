using System;
using Guts.Api.Models;

namespace Guts.Api.Tests.Builders
{
    internal class LoginModelBuilder
    {
        private readonly LoginModel _model;

        public LoginModelBuilder()
        {
            _model = new LoginModel
            {
                Email = Guid.NewGuid().ToString(),
                Password = Guid.NewGuid().ToString(),
                LoginSessionPublicIdentifier = null
            };
        }

        public LoginModelBuilder WithSession()
        {
            _model.LoginSessionPublicIdentifier = Guid.NewGuid().ToString();
            return this;
        }

        public LoginModel Build()
        {
            return _model;
        }

    }
}