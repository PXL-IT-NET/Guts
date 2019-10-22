using System.Collections.Generic;
using Guts.Business.Dtos;
using Guts.Domain.AssignmentAggregate;
using Guts.Domain.TestRunAggregate;

namespace Guts.Api.Models.Converters
{
    public interface IAssignmentConverter
    {
        AssignmentDetailModel ToAssignmentDetailModel(Assignment assignment, IList<TestResult> results, AssignmentTestRunInfoDto testRunInfo);
    }
}