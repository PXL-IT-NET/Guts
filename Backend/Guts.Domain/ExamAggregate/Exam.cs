using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Guts.Common;
using Guts.Domain.CourseAggregate;

namespace Guts.Domain.ExamAggregate
{
    public class Exam : AggregateRoot
    {
        private readonly HashSet<ExamPart> _parts;

        public virtual Course Course { get; private set; }
        public int CourseId { get; private set; }

        [Required]
        public string Name { get; private set; }

        public int MaximumScore { get; private set; }

        public virtual IReadOnlyCollection<ExamPart> Parts => _parts;

        private Exam(int courseId, string name)
        {
            Contracts.Require(courseId > 0, "The courseId must be a positive number.");
            Contracts.Require(!string.IsNullOrEmpty(name), "The name cannot be empty.");

            CourseId = courseId;
            Name = name;
            MaximumScore = 20;

            _parts = new HashSet<ExamPart>();
        }

        public ExamPart AddExamPart(string name, DateTime deadline)
        {
            Contracts.Require(deadline > DateTime.UtcNow, "The deadline of a new exam part must be in the future.");
            var evaluation = new ExamPart(Id, name, deadline);
            _parts.Add(evaluation);
            return evaluation;
        }

        public class Factory : IExamFactory
        {
            public Exam CreateNew(int courseId, string name)
            {
                
                return new Exam(courseId, name);
            }
        }
    }
}