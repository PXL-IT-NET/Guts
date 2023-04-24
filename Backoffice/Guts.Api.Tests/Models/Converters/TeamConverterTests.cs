using System;
using System.Collections.Generic;
using System.Linq;
using Guts.Api.Models.Converters;
using Guts.Business.Tests.Builders;
using Guts.Domain.ProjectTeamAggregate;
using Guts.Domain.UserAggregate;
using NUnit.Framework;

namespace Guts.Api.Tests.Models.Converters
{
    [TestFixture]
    internal class TeamConverterTests
    {
        private TeamConverter _converter;

        [SetUp]
        public void Setup()
        {
            _converter = new TeamConverter();
        }

        [Test]
        public void ToTeamDetailsModel_ShouldCorrectlyConvertValidProjectTeam()
        {
            //Arrange
            var team = new ProjectTeamBuilder().WithId().WithUsers(2).Build();

            //Act
            var model = _converter.ToTeamDetailsModel(team);

            //Assert
            Assert.That(model, Is.Not.Null);
            Assert.That(model.Id, Is.EqualTo(team.Id));
            Assert.That(model.Name, Is.EqualTo(team.Name));

            foreach (var teamUser in team.TeamUsers)
            {
                User user = teamUser.User;
                var matchingTeamUserModel = model.Members.FirstOrDefault(m => m.UserId == user.Id);
                Assert.That(matchingTeamUserModel, Is.Not.Null);
                Assert.That(matchingTeamUserModel.Name, Is.EqualTo(user.FirstName + " " + user.LastName));
            }
        }

        [Test]
        public void ToTeamDetailsModel_ShouldThrowArgumentExceptionWhenTeamUsersAreMissing()
        {
            //Arrange
            var team = new ProjectTeamBuilder().WithId().Build();
            team.TeamUsers = null;

            //Act + Assert
            Assert.That(() => _converter.ToTeamDetailsModel(team), Throws.InstanceOf<ArgumentException>());
        }

        [Test]
        public void ToTeamDetailsModel_ShouldThrowArgumentExceptionWhenUsersAreMissing()
        {
            //Arrange
            var team = new ProjectTeamBuilder().WithId().Build();
            team.TeamUsers = new List<IProjectTeamUser>
            {
                new ProjectTeamUser
                {
                    User = null
                }
            };

            //Act + Assert
            Assert.That(() => _converter.ToTeamDetailsModel(team), Throws.InstanceOf<ArgumentException>());
        }
    }
}