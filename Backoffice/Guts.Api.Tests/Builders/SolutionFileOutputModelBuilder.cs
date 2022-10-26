using System;
using Guts.Api.Models;

namespace Guts.Api.Tests.Builders
{
    internal class SolutionFileOutputModelBuilder
    {
        private readonly SolutionFileOutputModel _outputModel;

        public SolutionFileOutputModelBuilder()
        {
            _outputModel = new SolutionFileOutputModel
            {
                Content = Guid.NewGuid().ToString(),
                FilePath = Guid.NewGuid().ToString()
            };
        }

        public SolutionFileOutputModel Build()
        {
            return _outputModel;
        }
    }
}