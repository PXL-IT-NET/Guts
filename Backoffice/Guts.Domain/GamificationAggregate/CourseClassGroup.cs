using Guts.Common;
using Guts.Domain.CourseAggregate;
using Guts.Domain.PeriodAggregate;

namespace Guts.Domain.GamificationAggregate
{
    public class CourseClassGroup : AggregateRoot
    {
        private string _name;

        public string Name
        {
            get => _name;
            set
            {
                Contracts.Require(!string.IsNullOrWhiteSpace(value), "Class name cannot be empty.");
                _name = value!.Trim();
            }
        }

        public int CourseId { get; set; }
        public Course Course { get; set; }

        public int PeriodId { get; set; }
        public Period Period { get; set; }

        public CourseClassGroup() { }

        public CourseClassGroup(string name, int courseId, int periodId)
        {
            Contracts.Require(!string.IsNullOrWhiteSpace(name), "Class name cannot be empty.");
            Contracts.Require(courseId > 0, "CourseId must be positive.");
            Contracts.Require(periodId > 0, "PeriodId must be positive.");

            _name = name!.Trim();
            CourseId = courseId;
            PeriodId = periodId;
        }

        public class Factory : ICourseClassGroupFactory
        {
            public CourseClassGroup CreateNew(string name, int courseId, int periodId)
            {
                return new CourseClassGroup(name, courseId, periodId);
            }
        }
    }
}
