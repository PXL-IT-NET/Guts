using System;
using Guts.Api.Models;

namespace Guts.Api.Tests.Builders
{
    internal class RegisterModelBuilder
    {
        private readonly RegisterModel _model;

        public RegisterModelBuilder()
        {
            _model = new RegisterModel
            {
                Email = Guid.NewGuid().ToString(),
                Password = Guid.NewGuid().ToString(),
                CaptchaToken = Guid.NewGuid().ToString()
            };
        }

        public RegisterModelBuilder WithEmail(string email)
        {
            _model.Email = email;
            return this;
        }

        public RegisterModelBuilder WithValidEmail()
        {
            _model.Email = string.Concat(Guid.NewGuid().ToString(), "@student.pxl.be");
            return this;
        }

        public RegisterModel Build()
        {
            return _model;
        }

    }
}