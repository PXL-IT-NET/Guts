using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Guts.Common;
using Guts.Domain.CourseAggregate;
using Guts.Domain.PeriodAggregate;
using Guts.Domain.UserAggregate;

namespace Guts.Domain.ExamAggregate
{
    public class Exam : AggregateRoot
    {
        private readonly HashSet<IExamPart> _parts;

        public virtual Course Course { get; private set; }
        public int CourseId { get; private set; }

        public virtual Period Period { get; private set; }
        public int PeriodId { get; set; }

        [Required]
        public string Name { get; private set; }

        public int MaximumScore { get; private set; } //TODO: rename to NormalizedMaximumScore

        public IReadOnlyCollection<IExamPart> Parts => _parts;

        private Exam(int courseId, int periodId, string name)
        {
            Contracts.Require(courseId > 0, "The course Id must be a positive number.");
            Contracts.Require(periodId > 0, "The period Id must be a positive number.");
            Contracts.Require(!string.IsNullOrEmpty(name), "The name cannot be empty.");

            CourseId = courseId;
            PeriodId = periodId;
            Name = name;
            MaximumScore = 20;

            _parts = new HashSet<IExamPart>();
        }

        public ExamPart AddExamPart(string name, DateTime deadline)
        {
            var evaluation = new ExamPart(Id, name, deadline);
            _parts.Add(evaluation);
            return evaluation;
        }

        public IExamScore CalculateScoreForUser(User user, IExamTestResultCollection examTestResults)
        {
            var examScoreOfUser = new ExamScore(user, this);

            foreach (var examPart in Parts)
            {
                var examPartScore =
                    examPart.CalculateScoreForUser(user.Id, examTestResults.GetExamPartResults(examPart.Id));
                examScoreOfUser.AddExamPartScore(examPartScore);
            }

            return examScoreOfUser;
        }

        public class Factory : IExamFactory
        {
            public Exam CreateNew(int courseId, int periodId, string name)
            {

                return new Exam(courseId, periodId, name);
            }
        }
    }
}