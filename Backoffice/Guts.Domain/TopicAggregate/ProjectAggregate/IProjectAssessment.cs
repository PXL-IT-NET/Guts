using System;
using Guts.Domain.ProjectTeamAssessmentAggregate;

namespace Guts.Domain.TopicAggregate.ProjectAggregate
{
    /// <summary>
    /// A planned peer assessment window for a project.
    /// A project can have multiple assessments planned (e.g. one after each intermediate project deadline).
    /// Multiple <see cref="IProjectTeamAssessment">team assessments</see> will be made for each project assessment (preferably one team assessment for each team).
    /// </summary>
    public interface IProjectAssessment : IEntity
    {
        /// <summary>
        /// Unique identifier of the project.
        /// </summary>
        int ProjectId { get; }

        /// <summary>
        /// Describes the purpose of this particular project assessment (e.g. Intermediary evaluation after sprint 2).
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Utc date on which teams can create <see cref="IProjectTeamAssessment">team assessments</see> for this project assessment.
        /// </summary>
        DateTime OpenOnUtc { get; }

        /// <summary>
        /// Utc date until teams can create and modify <see cref="IProjectTeamAssessment">team assessments</see> for this project assessment.
        /// </summary>
        DateTime DeadlineUtc { get; }
    }
}