using System;
using Guts.Api.Models;

namespace Guts.Api.Tests.Builders
{
    internal class SolutionFileModelBuilder
    {
        private readonly SolutionFileModel _model;

        public SolutionFileModelBuilder()
        {
            _model = new SolutionFileModel
            {
                Content = Guid.NewGuid().ToString(),
                FilePath = Guid.NewGuid().ToString()
            };
        }

        public SolutionFileModel Build()
        {
            return _model;
        }
    }
}