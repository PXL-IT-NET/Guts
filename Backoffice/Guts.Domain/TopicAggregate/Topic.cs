using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Guts.Domain.AssignmentAggregate;
using Guts.Domain.CourseAggregate;
using Guts.Domain.PeriodAggregate;
using Guts.Domain.ValueObjects;

namespace Guts.Domain.TopicAggregate
{
    public class Topic : AggregateRoot, ITopic //TODO: make internal
    {
        private string _code;

        public Code Code
        {
            get => _code;
            set
            {
                _code = value;
                if (string.IsNullOrEmpty(Description))
                {
                    Description = value;
                }
            }
        }

        public string Description { get; set; }

        public virtual Course Course { get; set; }
        public int CourseId { get; set; }

        public virtual Period Period { get; set; }
        public int PeriodId { get; set; }

        public virtual ICollection<Assignment> Assignments { get; set; } = new HashSet<Assignment>();

        protected Topic()
        {
        }

        protected Topic(int id) : base(id)
        {
            
        }
    }
}