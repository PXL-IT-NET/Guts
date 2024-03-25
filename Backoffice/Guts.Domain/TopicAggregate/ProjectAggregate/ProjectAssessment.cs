using System;
using Guts.Common;

namespace Guts.Domain.TopicAggregate.ProjectAggregate
{
    internal class ProjectAssessment : Entity, IProjectAssessment
    {
        public int ProjectId { get; private set; }
        public string Description { get; private set; }

        public DateTime OpenOnUtc { get; private set; }

        public DateTime DeadlineUtc { get; private set; }

        private ProjectAssessment() { } //Needed for EF

        public void Update(string description, DateTime openOnUtc, DateTime deadlineUtc)
        {
            Description = description;
            OpenOnUtc = openOnUtc;
            DeadlineUtc = deadlineUtc;
            Validate();
        }

        private void Validate()
        {
            Contracts.Require(!string.IsNullOrEmpty(Description), "The description of a project assessment cannot be empty");
            Contracts.Require(OpenOnUtc.Kind == DateTimeKind.Utc, "The opening date must be a UTC date.");
            Contracts.Require(OpenOnUtc < DeadlineUtc, "The deadline date cannot be before the opening date.");
            Contracts.Require(DeadlineUtc.Kind == DateTimeKind.Utc, "The deadline date must be a UTC date.");
        }

        public class Factory : IProjectAssessmentFactory
        {
            public IProjectAssessment CreateNew(int projectId, string description, DateTime openOnUtc, DateTime deadlineUtc)
            {
                Contracts.Require(projectId > 0, "A project assessment can only be created for an existing (stored) project.");
                Contracts.Require(deadlineUtc > DateTime.UtcNow, "The deadline date must be in the future.");

                var assessment = new ProjectAssessment
                {
                    ProjectId = projectId,
                    Description = description,
                    OpenOnUtc = openOnUtc,
                    DeadlineUtc = deadlineUtc
                };
                assessment.Validate();

                return assessment;
            }
        }
    }
}