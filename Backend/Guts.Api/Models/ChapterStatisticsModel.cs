using System.Collections.Generic;

namespace Guts.Api.Models
{
    public class ChapterStatisticsModel
    {
        public int Id { get; set; }
        public int Number { get; set; }
        public IList<ExerciseStatisticsModel> ExerciseStatistics { get; set; }
    }
}