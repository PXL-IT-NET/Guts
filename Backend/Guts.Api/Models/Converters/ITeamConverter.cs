using Guts.Domain;

namespace Guts.Api.Models.Converters
{
    public interface ITeamConverter
    {
        TeamDetailsModel ToTeamDetailsModel(ProjectTeam projectTeam);
    }
}