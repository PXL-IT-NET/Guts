using System.Collections.Generic;
using Guts.Business;
using Guts.Domain;

namespace Guts.Api.Models.Converters
{
    public interface IAssignmentConverter
    {
        AssignmentDetailModel ToAssignmentDetailModel(Assignment assignment, IList<TestResult> results, AssignmentTestRunInfoDto testRunInfo);
    }
}