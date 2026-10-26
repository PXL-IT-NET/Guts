using System;
using System.Collections.Generic;
using System.Linq;
using Guts.Domain.ProjectTeamAggregate;

using Guts.Api.Models.ProjectModels;

namespace Guts.Api.Models.Converters;

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

        foreach (var user in projectTeam.TeamUsers.Select(teamUser => teamUser.User))
        {
            if (user == null)
            {
                throw new ArgumentException("The users should be loaded");
            }

            var member = new TeamUserModel
            {
                UserId = user.Id,
                Name = (user.FirstName + " " + user.LastName).Trim()
            };
            model.Members.Add(member);
        }

        return model;
    }
}