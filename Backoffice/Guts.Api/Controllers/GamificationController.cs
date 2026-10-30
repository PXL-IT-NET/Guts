using Guts.Business.Communication;
using Guts.Business.Repositories;
using Guts.Domain.UserAggregate;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Guts.Business.Dtos.Gamification;
using Guts.Business.Services;
using Guts.Domain.GamificationAggregate;
using Guts.Domain.RoleAggregate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Guts.Api.Controllers;

[Authorize]
[Produces("application/json")]
[Route("api/courses/{courseId}/gamification")]
public class GamificationController(
    IGamificationService gamificationService,
    IMailSender mailSender,
    ICourseRepository courseRepository) : ControllerBase
{
    [HttpGet("profile")]
    [ProducesResponseType(typeof(StudentGamificationProfileDto), (int)HttpStatusCode.OK)]
    public async Task<IActionResult> GetProfile(
        int courseId,
        [FromQuery] int? periodId = null,
        [FromQuery] int? perspectiveUserId = null)
    {
        int userId = GetUserId();
        bool isLector = IsLector();
        var profile = await gamificationService.GetStudentCourseProfileAsync(
            userId,
            courseId,
            periodId,
            isLector,
            isLector ? perspectiveUserId : null);
        return Ok(profile);
    }

    [HttpPost("enroll")]
    [ProducesResponseType(typeof(StudentGamificationProfileDto), (int)HttpStatusCode.OK)]
    public async Task<IActionResult> Enroll(int courseId, [FromBody] EnrollStudentDto enrollDto, [FromQuery] int? periodId = null)
    {
        int userId = GetUserId();
        var updatedProfile = await gamificationService.EnrollStudentAsync(userId, courseId, enrollDto, periodId);
        return Ok(updatedProfile);
    }

    [HttpGet("leaderboard/weekly")]
    [ProducesResponseType(typeof(IReadOnlyList<LeaderboardEntryDto>), (int)HttpStatusCode.OK)]
    public async Task<IActionResult> GetWeeklySprint(
        int courseId,
        [FromQuery] int? periodId = null,
        [FromQuery] int? perspectiveUserId = null)
    {
        int effectiveUserId = IsLector() && perspectiveUserId.HasValue
            ? perspectiveUserId.Value
            : GetUserId();
        var leaderboard = await gamificationService.GetWeeklySprintLeaderboardAsync(courseId, effectiveUserId, periodId);
        return Ok(leaderboard);
    }

    [HttpGet("leaderboard/hall-of-fame")]
    [ProducesResponseType(typeof(HallOfFameLeaderboardDto), (int)HttpStatusCode.OK)]
    public async Task<IActionResult> GetHallOfFame(
        int courseId,
        [FromQuery] int? periodId = null,
        [FromQuery] int? perspectiveUserId = null,
        [FromQuery] HallOfFameTier? tier = null)
    {
        int userId = GetUserId();
        bool isLector = IsLector();
        var leaderboard = await gamificationService.GetHallOfFameLeaderboardAsync(
            courseId,
            userId,
            periodId,
            isLector,
            isLector ? perspectiveUserId : null,
            isLector ? tier : null);
        return Ok(leaderboard);
    }

    [HttpGet("leaderboard/classes")]
    [ProducesResponseType(typeof(IReadOnlyList<ClassGroupLeaderboardEntryDto>), (int)HttpStatusCode.OK)]
    public async Task<IActionResult> GetClassLeaderboard(int courseId, [FromQuery] int? periodId = null)
    {
        var leaderboard = await gamificationService.GetClassGroupLeaderboardAsync(courseId, periodId);
        return Ok(leaderboard);
    }

    [HttpGet("config")]
    [ProducesResponseType(typeof(CourseGamificationConfigDto), (int)HttpStatusCode.OK)]
    public async Task<IActionResult> GetConfig(int courseId)
    {
        var config = await gamificationService.GetCourseConfigAsync(courseId);
        return Ok(config);
    }

    [HttpPut("config")]
    [Authorize(Roles = Role.Constants.Lector)]
    [ProducesResponseType(typeof(CourseGamificationConfigDto), (int)HttpStatusCode.OK)]
    public async Task<IActionResult> UpdateConfig(int courseId, [FromBody] CourseGamificationConfigDto configDto)
    {
        var updated = await gamificationService.UpdateCourseConfigAsync(courseId, configDto);
        return Ok(updated);
    }

    [HttpGet("classes")]
    [ProducesResponseType(typeof(IReadOnlyList<CourseClassGroupDto>), (int)HttpStatusCode.OK)]
    public async Task<IActionResult> GetClasses(int courseId, [FromQuery] int? periodId = null)
    {
        var classes = await gamificationService.GetCourseClassGroupsAsync(courseId, periodId);
        return Ok(classes);
    }

    [HttpPost("classes")]
    [Authorize(Roles = Role.Constants.Lector)]
    [ProducesResponseType(typeof(CourseClassGroupDto), (int)HttpStatusCode.OK)]
    public async Task<IActionResult> AddClass(int courseId, [FromBody] string name, [FromQuery] int? periodId = null)
    {
        var created = await gamificationService.AddCourseClassGroupAsync(courseId, name, periodId);
        return Ok(created);
    }

    [HttpDelete("classes/{classGroupId}")]
    [Authorize(Roles = Role.Constants.Lector)]
    [ProducesResponseType((int)HttpStatusCode.NoContent)]
    public async Task<IActionResult> DeleteClass(int courseId, int classGroupId)
    {
        await gamificationService.DeleteCourseClassGroupAsync(courseId, classGroupId);
        return NoContent();
    }

    [HttpGet("badges")]
    [HttpGet("/api/gamification/badges")]
    [ProducesResponseType(typeof(IReadOnlyList<BadgeMetadataDto>), (int)HttpStatusCode.OK)]
    public IActionResult GetBadgeMetadata()
    {
        var badges = gamificationService.GetBadgeMetadata();
        return Ok(badges);
    }

    [HttpPost("streak-reminders/test-email")]
    [Authorize(Roles = Role.Constants.Lector)]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    public async Task<IActionResult> SendTestStreakReminderEmail(
        int courseId,
        [FromQuery] string recipientEmail = null)
    {
        string targetEmail = !string.IsNullOrWhiteSpace(recipientEmail)
            ? recipientEmail.Trim()
            : GetUserEmail();

        if (string.IsNullOrWhiteSpace(targetEmail))
        {
            return BadRequest("A recipient email address must be provided or present in the authenticated user claims.");
        }

        string courseName = $"Course #{courseId}";
        try
        {
            var course = await courseRepository.GetByIdAsync(courseId);
            if (course != null && !string.IsNullOrWhiteSpace(course.Name))
            {
                courseName = course.Name;
            }
        }
        catch
        {
            // Fallback to generic course name if course cannot be retrieved
        }

        var sampleUser = new User
        {
            Id = GetUserId(),
            FirstName = "Teacher",
            LastName = "Test",
            Email = targetEmail
        };

        var sampleRecommendations = new List<StreakReminderAssignmentDto>
        {
            new()
            {
                AssignmentId = 1,
                TopicDescription = "Chapter 1: Getting Started",
                AssignmentDescription = "Exercise 1.1 First Steps",
                TopicCode = "Ch1",
                GreenTestsCount = 2,
                TotalTestsCount = 5
            },
            new()
            {
                AssignmentId = 2,
                TopicDescription = "Chapter 1: Getting Started",
                AssignmentDescription = "Exercise 1.2 Control Structures",
                TopicCode = "Ch1",
                GreenTestsCount = 0,
                TotalTestsCount = 4
            }
        };

        await mailSender.SendStreakReminderEmailAsync(
            sampleUser,
            courseName,
            courseId,
            currentStreak: 3,
            targetStudentName: "CodeMaster99",
            sampleRecommendations);

        return Ok(new { message = $"Test streak reminder email sent to {targetEmail}", recipientEmail = targetEmail, courseName });
    }
}
