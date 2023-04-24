using Guts.Api.Models.ProjectModels;
using Guts.Domain.ProjectTeamAggregate;

namespace Guts.Api.Models.Converters
{
    public interface ITeamConverter
    {
        TeamDetailsModel ToTeamDetailsModel(IProjectTeam projectTeam);
    }
}