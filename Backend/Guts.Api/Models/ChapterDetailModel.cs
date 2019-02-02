using System.Collections.Generic;

namespace Guts.Api.Models
{
    public class ChapterDetailModel : TopicModel
    {
        public IList<AssignmentModel> Exercises { get; set; }
        public IList<UserModel> Users { get; set; }
    }
}