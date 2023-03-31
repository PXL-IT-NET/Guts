using Guts.Common.Extensions;
using System;
using System.Collections.ObjectModel;
using Guts.Domain.ProjectTeamAggregate;
using Guts.Domain.Tests.Builders;
using Guts.Domain.TopicAggregate.ProjectAggregate;

namespace Guts.Business.Tests.Builders
{
    public class ProjectTeamBuilder
    {
        private readonly Random _random;
        private readonly ProjectTeam _team;

        public ProjectTeamBuilder()
        {
            _random = new Random();
            _team = new ProjectTeam
            {
                Id = 0,
                Name = Guid.NewGuid().ToString(),
                TeamUsers = new Collection<IProjectTeamUser>()
            };
        }

        public ProjectTeamBuilder WithId()
        {
            _team.Id = _random.NextPositive();
            return this;
        }

        public ProjectTeamBuilder WithName(string name)
        {
            _team.Name = name;
            return this;
        }

        public ProjectTeamBuilder WithProject(Project project)
        {
            _team.ProjectId = project.Id;
            _team.Project = project;
            return this;
        }

        public ProjectTeamBuilder WithUsers(int numberOfUsers)
        {
            for (int i = 0; i < numberOfUsers; i++)
            {
                var teamUser = new UserBuilder().Build();
                _team.TeamUsers.Add(new ProjectTeamUser
                {
                    ProjectTeamId = _team.Id,
                    UserId = teamUser.Id,
                    User = teamUser
                });
            }
            return this;
        }

        public ProjectTeamBuilder WithUser(int userId)
        {
            var teamUser = new UserBuilder().WithId(userId).Build();
            _team.TeamUsers.Add(new ProjectTeamUser
            {
                ProjectTeamId = _team.Id,
                UserId = teamUser.Id,
                User = teamUser
            });
            return this;
        }

        public ProjectTeam Build()
        {
            return _team;
        }
    }
}