using System.Collections.Generic;

namespace Guts.Api.Models
{
    public class TeamDetailsModel : TeamModel
    {
        public IList<string> Members { get; set; }
    }
}