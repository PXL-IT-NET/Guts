using Guts.Domain.ProjectTeamAggregate;

namespace Guts.Api.Models.Converters
{
    public interface ITeamConverter
    {
        TeamDetailsModel ToTeamDetailsModel(IProjectTeam projectTeam);
    }
}