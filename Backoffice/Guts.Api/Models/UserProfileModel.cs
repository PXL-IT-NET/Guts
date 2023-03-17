using System.Collections.Generic;

namespace Guts.Api.Models
{
    public class UserProfileModel
    {
        public int Id { get; set; }
        public IList<string> Roles { get; set; }
        public List<int> Teams { get; set; }
    }
}