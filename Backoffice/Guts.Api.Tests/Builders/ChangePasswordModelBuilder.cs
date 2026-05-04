using System;
using Guts.Api.Models;

namespace Guts.Api.Tests.Builders
{
    internal class ChangePasswordModelBuilder
    {
        private readonly ChangePasswordModel _model;

        public ChangePasswordModelBuilder()
        {
            _model = new ChangePasswordModel
            {
                CurrentPassword = Guid.NewGuid().ToString(),
                NewPassword = Guid.NewGuid().ToString()
            };
        }

        public ChangePasswordModel Build()
        {
            return _model;
        }
    }
}