using System;
using Guts.Api.Models;

namespace Guts.Api.Tests.Builders
{
    internal class ResetPasswordModelBuilder
    {
        private readonly ResetPasswordModel _model;

        public ResetPasswordModelBuilder()
        {
            _model = new ResetPasswordModel
            {
                UserId = Guid.NewGuid().ToString(),
                Token = Guid.NewGuid().ToString(),
                Password = Guid.NewGuid().ToString(),
            };
        }

        public ResetPasswordModel Build()
        {
            return _model;
        }

    }
}