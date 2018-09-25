using System.Collections.Generic;

namespace Guts.Api.Models
{
    public class ChapterDetailModel : ChapterModel
    {
        public IList<ExerciseModel> Exercises { get; set; }
        public IList<UserModel> Users { get; set; }
    }
}