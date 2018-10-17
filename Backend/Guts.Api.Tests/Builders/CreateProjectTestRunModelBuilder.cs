using System.Collections.Generic;
using Guts.Api.Models;
using Guts.Business;
using Guts.Business.Tests.Builders;

namespace Guts.Api.Tests.Builders
{
    internal class CreateProjectTestRunModelBuilder : CreateTestRunModelBuilderBase<CreateProjectTestRunModel>
    {
        public CreateProjectTestRunModelBuilder()
        {
            Model = new CreateProjectTestRunModel
            {
                ProjectComponent = new ProjectComponentDtoBuilder().Build(),
                Results = new List<TestResultModel>(),
                SourceCode = null
            };
        }

        public CreateProjectTestRunModelBuilder WithProjectComponent(ProjectComponentDto projectComponentDto)
        {
            Model.ProjectComponent = projectComponentDto;
            return this;
        }
    }
}