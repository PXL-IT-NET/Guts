using System.Collections.Generic;

namespace Guts.Api.Models.ExamModels
{
    public class ExamOutputModel
    {
        public int Id { get; set; }
        public int CourseId { get; set; }
        public string Name { get; set; }
        public int MaximumScore { get; set; }
        public IList<ExamPartOutputModel> Parts { get; set; }
    }
}