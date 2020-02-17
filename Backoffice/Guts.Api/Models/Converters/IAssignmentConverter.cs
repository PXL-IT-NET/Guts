using System.Collections.Generic;
using Guts.Business.Dtos;
using Guts.Domain.AssignmentAggregate;
using Guts.Domain.TestRunAggregate;
using Guts.Domain.ValueObjects;

namespace Guts.Api.Models.Converters
{
    public interface IAssignmentConverter
    {
        AssignmentDetailModel ToAssignmentDetailModel(
            Assignment assignment,
            AssignmentTestRunInfoDto testRunInfo,
            IList<TestResult> results,
            IList<SolutionFile> solutionFiles);
    }
}