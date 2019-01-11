using System;
using Guts.Common.Extensions;

namespace Guts.Business.Tests.Builders
{
    public class AssignmentDtoBuilder
    {
        private readonly AssignmentDto _dto;

        public AssignmentDtoBuilder()
        {
            _dto = new AssignmentDto
            {
                CourseCode = Guid.NewGuid().ToString(),
                TopicCode = Guid.NewGuid().ToString(),
                AssignmentCode = Guid.NewGuid().ToString()
            };
        }

        public AssignmentDtoBuilder WithAssignmentCode(string code)
        {
            _dto.AssignmentCode = code;
            return this;
        }

        public AssignmentDto Build()
        {
            return _dto;
        }
    }
}