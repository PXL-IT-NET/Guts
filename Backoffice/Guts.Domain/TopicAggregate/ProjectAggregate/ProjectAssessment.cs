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

        public class Factory : IProjectAssessmentFactory
        {
            public IProjectAssessment CreateNew(int projectId, string description, DateTime openOnUtc, DateTime deadlineUtc)
            {
                Contracts.Require(projectId > 0, "A project assessment can only be created for an existing (stored) project.");
                Contracts.Require(!string.IsNullOrEmpty(description), "The description of a project assessment cannot be empty");
                Contracts.Require(openOnUtc.Kind == DateTimeKind.Utc, "The opening date must be a UTC date.");
                Contracts.Require(openOnUtc < deadlineUtc, "The deadline date cannot be before the opening date.");
                Contracts.Require(deadlineUtc.Kind == DateTimeKind.Utc, "The deadline date must be a UTC date.");
                Contracts.Require(deadlineUtc > DateTime.UtcNow, "The deadline date must be in the future.");

                var assessment = new ProjectAssessment
                {
                    ProjectId = projectId,
                    Description = description,
                    OpenOnUtc = openOnUtc,
                    DeadlineUtc = deadlineUtc
                };

                return assessment;
            }
        }
    }
}