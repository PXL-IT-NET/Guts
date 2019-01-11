using System.Collections.Generic;

namespace Guts.Api.Models
{
    public class ChapterSummaryModel
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string Description { get; set; }
        public IList<AssignmentSummaryModel> ExerciseSummaries { get; set; }
    }
}
