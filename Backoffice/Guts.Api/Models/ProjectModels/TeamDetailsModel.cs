using System.Collections.Generic;

namespace Guts.Api.Models.ProjectModels
{
    public class TeamDetailsModel : TeamModel
    {
        public IList<TeamUserModel> Members { get; set; }
    }
}