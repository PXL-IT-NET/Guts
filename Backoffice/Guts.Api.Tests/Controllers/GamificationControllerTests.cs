using System.Collections.Generic;
using System.Threading.Tasks;
using Guts.Api.Controllers;
using Guts.Api.Tests.Builders;
using Guts.Business.Dtos.Gamification;
using Guts.Business.Services;
using Guts.Domain.GamificationAggregate;
using Guts.Domain.RoleAggregate;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;

namespace Guts.Api.Tests.Controllers;

[TestFixture]
public class GamificationControllerTests
{
    private GamificationController _controller;
    private Mock<IGamificationService> _gamificationServiceMock;
    private const int UserId = 123;
    private const int CourseId = 456;
    private const int PeriodId = 789;

    [SetUp]
    public void Setup()
    {
        _gamificationServiceMock = new Mock<IGamificationService>();
        _controller = new GamificationController(_gamificationServiceMock.Object)
        {
            ControllerContext = new ControllerContextBuilder().WithUser(UserId.ToString()).Build()
        };
    }

    [Test]
    public async Task GetProfile_ShouldReturnProfileFromService()
    {
        var profileDto = new StudentGamificationProfileDto
        {
            UserId = UserId,
            CourseId = CourseId,
            PeriodId = PeriodId,
            TotalXp = 150,
            ShowInLeaderBoard = true
        };
        _gamificationServiceMock.Setup(s => s.GetStudentCourseProfileAsync(UserId, CourseId, PeriodId)).ReturnsAsync(profileDto);

        var actionResult = await _controller.GetProfile(CourseId, PeriodId);

        var okResult = actionResult as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        Assert.That(okResult.Value, Is.EqualTo(profileDto));
    }

    [Test]
    public async Task Enroll_ShouldCallEnrollAndReturnUpdatedProfile()
    {
        var enrollDto = new EnrollStudentDto
        {
            ShowInLeaderBoard = true,
            UsePseudonym = true,
            Pseudonym = "BitHero",
            Avatar = "Rocket"
        };

        var updatedProfile = new StudentGamificationProfileDto
        {
            UserId = UserId,
            CourseId = CourseId,
            ShowInLeaderBoard = true,
            Pseudonym = "BitHero"
        };
        _gamificationServiceMock.Setup(s => s.EnrollStudentAsync(UserId, CourseId, enrollDto, PeriodId)).ReturnsAsync(updatedProfile);

        var actionResult = await _controller.Enroll(CourseId, enrollDto, PeriodId);

        var okResult = actionResult as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        Assert.That(okResult.Value, Is.EqualTo(updatedProfile));
    }

    [Test]
    public async Task GetWeeklySprint_ForStudent_ShouldIgnorePerspectiveUserIdAndUseCurrentUserId()
    {
        var leaderboard = new List<LeaderboardEntryDto>
        {
            new LeaderboardEntryDto { Rank = 1, UserId = 1, Score = 100 },
            new LeaderboardEntryDto { Rank = 2, UserId = UserId, Score = 50 }
        };
        _gamificationServiceMock.Setup(s => s.GetWeeklySprintLeaderboardAsync(CourseId, UserId, PeriodId)).ReturnsAsync(leaderboard);

        var actionResult = await _controller.GetWeeklySprint(CourseId, PeriodId, perspectiveUserId: 999);

        var okResult = actionResult as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        Assert.That(okResult.Value, Is.EqualTo(leaderboard));
    }

    [Test]
    public async Task GetWeeklySprint_ForLectorWithPerspectiveUserId_ShouldPassPerspectiveUserId()
    {
        _controller.ControllerContext = new ControllerContextBuilder()
            .WithUser(UserId.ToString())
            .WithRole(Role.Constants.Lector)
            .Build();

        const int perspectiveUserId = 555;
        var leaderboard = new List<LeaderboardEntryDto>
        {
            new LeaderboardEntryDto { Rank = 1, UserId = perspectiveUserId, Score = 100, IsCurrentUser = true }
        };
        _gamificationServiceMock.Setup(s => s.GetWeeklySprintLeaderboardAsync(CourseId, perspectiveUserId, PeriodId))
            .ReturnsAsync(leaderboard);

        var actionResult = await _controller.GetWeeklySprint(CourseId, PeriodId, perspectiveUserId);

        var okResult = actionResult as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        Assert.That(okResult.Value, Is.EqualTo(leaderboard));
    }

    [Test]
    public async Task GetHallOfFame_ForStudent_ShouldIgnoreLectorQueryParametersAndReturnLeaderboard()
    {
        var leaderboard = new HallOfFameLeaderboardDto
        {
            Entries = new List<LeaderboardEntryDto>
            {
                new LeaderboardEntryDto { Rank = 1, UserId = 1, Score = 500 }
            }
        };
        _gamificationServiceMock
            .Setup(s => s.GetHallOfFameLeaderboardAsync(CourseId, UserId, PeriodId, false, null, null))
            .ReturnsAsync(leaderboard);

        var actionResult = await _controller.GetHallOfFame(CourseId, PeriodId, perspectiveUserId: 77, tier: HallOfFameTier.Gold);

        var okResult = actionResult as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        Assert.That(okResult.Value, Is.EqualTo(leaderboard));
    }

    [Test]
    public async Task GetHallOfFame_ForLector_ShouldPassIsLectorPerspectiveUserIdAndTier()
    {
        _controller.ControllerContext = new ControllerContextBuilder()
            .WithUser(UserId.ToString())
            .WithRole(Role.Constants.Lector)
            .Build();

        const int selectedStudentId = 88;
        var leaderboard = new HallOfFameLeaderboardDto
        {
            CurrentUserRank = 18,
            Entries = new List<LeaderboardEntryDto>
            {
                new LeaderboardEntryDto { Rank = 18, UserId = selectedStudentId, Score = 250, IsCurrentUser = true }
            }
        };
        _gamificationServiceMock
            .Setup(s => s.GetHallOfFameLeaderboardAsync(CourseId, UserId, PeriodId, true, selectedStudentId, HallOfFameTier.Diamond))
            .ReturnsAsync(leaderboard);

        var actionResult = await _controller.GetHallOfFame(CourseId, PeriodId, selectedStudentId, HallOfFameTier.Diamond);

        var okResult = actionResult as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        Assert.That(okResult.Value, Is.EqualTo(leaderboard));
    }

    [Test]
    public async Task GetClassLeaderboard_ShouldReturnClassRankings()
    {
        var rankings = new List<ClassGroupLeaderboardEntryDto>
        {
            new ClassGroupLeaderboardEntryDto { Rank = 1, ClassGroupName = "1TIN-A", AverageXp = 80.5 }
        };
        _gamificationServiceMock.Setup(s => s.GetClassGroupLeaderboardAsync(CourseId, PeriodId)).ReturnsAsync(rankings);

        var actionResult = await _controller.GetClassLeaderboard(CourseId, PeriodId);

        var okResult = actionResult as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        Assert.That(okResult.Value, Is.EqualTo(rankings));
    }

    [Test]
    public async Task AddClass_ShouldCallServiceAndReturnCreated()
    {
        const string className = "2AIT";
        var created = new CourseClassGroupDto { Id = 1, Name = className, CourseId = CourseId };
        _gamificationServiceMock.Setup(s => s.AddCourseClassGroupAsync(CourseId, className, PeriodId)).ReturnsAsync(created);

        var actionResult = await _controller.AddClass(CourseId, className, PeriodId);

        var okResult = actionResult as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        Assert.That(okResult.Value, Is.EqualTo(created));
    }

    [Test]
    public async Task DeleteClass_ShouldCallServiceAndReturnNoContent()
    {
        const int classGroupId = 7;
        var actionResult = await _controller.DeleteClass(CourseId, classGroupId);

        Assert.That(actionResult, Is.InstanceOf<NoContentResult>());
        _gamificationServiceMock.Verify(s => s.DeleteCourseClassGroupAsync(CourseId, classGroupId), Times.Once);
    }

    [Test]
    public void GetBadgeMetadata_ShouldCallServiceAndReturnOkWithBadges()
    {
        var badgeList = new List<BadgeMetadataDto>
        {
            new BadgeMetadataDto { BadgeType = BadgeType.FirstSpark, Name = "First Spark", Icon = "🌱" }
        };
        _gamificationServiceMock.Setup(s => s.GetBadgeMetadata()).Returns(badgeList);

        var actionResult = _controller.GetBadgeMetadata();

        var okResult = actionResult as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        Assert.That(okResult.Value, Is.EqualTo(badgeList));
    }

    [Test]
    public async Task GetConfig_ShouldReturnCourseConfigFromService()
    {
        var configDto = new CourseGamificationConfigDto
        {
            CourseId = CourseId,
            IsGamificationEnabled = true,
            IsLeaderboardEnabled = true
        };
        _gamificationServiceMock.Setup(s => s.GetCourseConfigAsync(CourseId)).ReturnsAsync(configDto);

        var actionResult = await _controller.GetConfig(CourseId);

        var okResult = actionResult as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        Assert.That(okResult.Value, Is.EqualTo(configDto));
    }

    [Test]
    public async Task UpdateConfig_ShouldCallServiceAndReturnUpdatedConfig()
    {
        var configDto = new CourseGamificationConfigDto
        {
            CourseId = CourseId,
            IsGamificationEnabled = false
        };
        _gamificationServiceMock.Setup(s => s.UpdateCourseConfigAsync(CourseId, configDto)).ReturnsAsync(configDto);

        var actionResult = await _controller.UpdateConfig(CourseId, configDto);

        var okResult = actionResult as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        Assert.That(okResult.Value, Is.EqualTo(configDto));
    }

    [Test]
    public async Task GetClasses_ShouldReturnCourseClassGroupsFromService()
    {
        var classes = new List<CourseClassGroupDto>
        {
            new CourseClassGroupDto { Id = 1, Name = "1TIN-A", CourseId = CourseId, PeriodId = PeriodId }
        };
        _gamificationServiceMock.Setup(s => s.GetCourseClassGroupsAsync(CourseId, PeriodId)).ReturnsAsync(classes);

        var actionResult = await _controller.GetClasses(CourseId, PeriodId);

        var okResult = actionResult as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        Assert.That(okResult.Value, Is.EqualTo(classes));
    }
}
