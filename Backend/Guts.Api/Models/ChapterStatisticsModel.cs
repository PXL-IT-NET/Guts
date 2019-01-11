using System.Collections.Generic;

namespace Guts.Api.Models
{
    public class ChapterStatisticsModel
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public IList<AssignmentStatisticsModel> ExerciseStatistics { get; set; }
    }
}