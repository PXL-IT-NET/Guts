using System.Collections.Generic;

namespace Guts.Api.Models
{
    public class SavedTestRunModel
    {
        public int Id { get; set; }
        public int ExerciseId { get; set; }
        public ICollection<SavedTestResultModel> TestResults { get; set; }
    }
}