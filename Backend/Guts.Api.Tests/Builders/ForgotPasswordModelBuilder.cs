using System;
using Guts.Api.Models;

namespace Guts.Api.Tests.Builders
{
    internal class ForgotPasswordModelBuilder
    {
        private readonly ForgotPasswordModel _model;

        public ForgotPasswordModelBuilder()
        {
            _model = new ForgotPasswordModel
            {
                Email = Guid.NewGuid().ToString(),
                CaptchaToken = Guid.NewGuid().ToString()
            };
        }

        public ForgotPasswordModelBuilder WithValidEmail()
        {
            _model.Email = string.Concat(Guid.NewGuid().ToString(), "@student.pxl.be");
            return this;
        }

        public ForgotPasswordModel Build()
        {
            return _model;
        }

       
    }
}