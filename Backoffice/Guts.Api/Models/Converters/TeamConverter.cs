using System;
using System.Collections.Generic;
using Guts.Domain.ProjectTeamAggregate;

using Guts.Api.Models.ProjectModels;

namespace Guts.Api.Models.Converters
{
    public class TeamConverter: ITeamConverter
    {
        public TeamDetailsModel ToTeamDetailsModel(IProjectTeam projectTeam)
        {
            if (projectTeam.TeamUsers == null)
            {
                throw new ArgumentException("The team users should be loaded");
            }

            var model = new TeamDetailsModel
            {
                Id = projectTeam.Id,
                Name = projectTeam.Name,
                Members = new List<TeamUserModel>()
            };

            foreach (var teamUser in projectTeam.TeamUsers)
            {
                if (teamUser.User == null)
                {
                    throw new ArgumentException("The users should be loaded");
                }

                var member = new TeamUserModel
                {
                    UserId = teamUser.User.Id,
                    Name = (teamUser.User.FirstName + " " + teamUser.User.LastName).Trim()
                };
                model.Members.Add(member);
            }

            return model;
        }
    }
}