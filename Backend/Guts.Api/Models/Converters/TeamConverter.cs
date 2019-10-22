using System;
using System.Collections.Generic;
using Guts.Domain.ProjectTeamAggregate;

namespace Guts.Api.Models.Converters
{
    public class TeamConverter: ITeamConverter
    {
        public TeamDetailsModel ToTeamDetailsModel(ProjectTeam projectTeam)
        {
            if (projectTeam.TeamUsers == null)
            {
                throw new ArgumentException("The team users should be loaded");
            }

            var model = new TeamDetailsModel
            {
                Id = projectTeam.Id,
                Name = projectTeam.Name,
                Members = new List<string>()
            };

            foreach (var teamUser in projectTeam.TeamUsers)
            {
                if (teamUser.User == null)
                {
                    throw new ArgumentException("The users should be loaded");
                }
                model.Members.Add(teamUser.User.FirstName + " " + teamUser.User.LastName);
            }

            return model;
        }
    }
}