using System.Collections.Generic;

namespace Guts.Api.Models
{
    public class ChapterDetailModel : ChapterModel
    {
        public IList<AssignmentModel> Exercises { get; set; }
        public IList<UserModel> Users { get; set; }
    }
}