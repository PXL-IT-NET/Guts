using System.Collections.Generic;

namespace Guts.Api.Models
{
    public class CourseContentsModel
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public IList<TopicModel> Chapters { get; set; }
        public IList<TopicModel> Projects { get; set; }
    }
}