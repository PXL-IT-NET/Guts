using System.Collections.Generic;

namespace Guts.Api.Models
{
    public class ChapterSummaryModel
    {
        public int Id { get; set; }
        public int Number { get; set; }
        public IList<ExerciseSummaryModel> UserExerciseSummaries { get; set; }
        public IList<ExerciseSummaryModel> AverageExerciseSummaries { get; set; }
    }
}
