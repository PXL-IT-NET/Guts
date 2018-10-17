using System;

namespace Guts.Business.Tests.Builders
{
    public class ProjectComponentDtoBuilder
    {
        private readonly ProjectComponentDto _dto;

        public ProjectComponentDtoBuilder()
        {
            _dto = new ProjectComponentDto
            {
                CourseCode = Guid.NewGuid().ToString(),
                ProjectCode = Guid.NewGuid().ToString(),
                ComponentCode = Guid.NewGuid().ToString()
            };
        }

        public ProjectComponentDtoBuilder WithComponentCode(string code)
        {
            _dto.ComponentCode = code;
            return this;
        }

        public ProjectComponentDto Build()
        {
            return _dto;
        }
    }
}