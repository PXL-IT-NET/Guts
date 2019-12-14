using System;
using Guts.Api.Models;

namespace Guts.Api.Tests.Builders
{
    internal class ConfirmEmailModelBuilder
    {
        private readonly ConfirmEmailModel _model;

        public ConfirmEmailModelBuilder()
        {
            _model = new ConfirmEmailModel
            {
                UserId = Guid.NewGuid().ToString(),
                Token = Guid.NewGuid().ToString()
            };
        }

        public ConfirmEmailModel Build()
        {
            return _model;
        }

    }
}