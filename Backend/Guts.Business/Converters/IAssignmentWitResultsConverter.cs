using Guts.Data;

namespace Guts.Business.Converters
{
    public interface IAssignmentWitResultsConverter
    {
        AssignmentResultDto ToAssignmentResultDto(AssignmentWithLastResultsOfUser assignmentWithLastResultsOfUser);
        AssignmentStatisticsDto ToAssignmentStatisticsDto(AssignmentWithLastResultsOfMultipleUsers assignmentWithLastResultsOfMultipleUsers);
    }
}
