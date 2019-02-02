using System;
using System.Collections.ObjectModel;
using Guts.Common.Extensions;
using Guts.Domain;

namespace Guts.Business.Tests.Builders
{
    public class ProjectBuilder
    {
        private readonly Random _random;
        private readonly Project _project;

        public ProjectBuilder()
        {
            _random = new Random();
            _project = new Project
            {
                Id = 0,
                CourseId = _random.NextPositive(),
                PeriodId = _random.NextPositive(),
                Description = Guid.NewGuid().ToString(),
                Assignments = new Collection<Assignment>(),
                Teams = new Collection<ProjectTeam>()
            };
        }

        public ProjectBuilder WithId()
        {
            _project.Id = _random.NextPositive();
            return this;
        }

        public ProjectBuilder WithCourseId(int courseId)
        {
            _project.CourseId = courseId;
            return this;
        }

        public ProjectBuilder WithDescription(string description)
        {
            _project.Description = description;
            return this;
        }

        public ProjectBuilder WithCourse()
        {
            _project.Course = new CourseBuilder().WithId().Build();
            _project.CourseId = _project.Course.Id;
            return this;
        }

        public ProjectBuilder WithCourse(string courseCode)
        {
            _project.Course = new CourseBuilder().WithId().WithCourseCode(courseCode).Build();
            _project.CourseId = _project.Course.Id;
            return this;
        }

        public ProjectBuilder WithoutCourseLoaded()
        {
            _project.Course = null;
            return this;
        }

        public ProjectBuilder WithPeriod(Period period)
        {
            _project.Period = period;
            _project.PeriodId = period.Id;
            return this;
        }

        public ProjectBuilder WithCode(string code)
        {
            _project.Code = code;
            return this;
        }

        public ProjectBuilder WithAssignments(int numberOfAssignments)
        {
            for (int i = 0; i < numberOfAssignments; i++)
            {
                var assignment = new AssignmentBuilder().WithId().WithTopic(_project).Build();
                _project.Assignments.Add(assignment);
            }
            return this;
        }

        public ProjectBuilder WithoutAssignments()
        {
            _project.Assignments = null;
            return this;
        }

        public ProjectBuilder WithTeams(int numberOfTeams)
        {
            for (int i = 0; i < numberOfTeams; i++)
            {
                var team = new ProjectTeamBuilder().WithId().WithProject(_project).Build();
                _project.Teams.Add(team);
            }
            return this;
        }

        public ProjectBuilder WithoutTeams()
        {
            _project.Teams = null;
            return this;
        }

        public Project Build()
        {
            return _project;
        }
    }
}