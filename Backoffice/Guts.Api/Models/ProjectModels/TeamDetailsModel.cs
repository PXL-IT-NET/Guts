using System.Collections.Generic;

namespace Guts.Api.Models.ProjectModels
{
    public class TeamDetailsModel : TeamModel
    {
        public IList<string> Members { get; set; }
    }
}