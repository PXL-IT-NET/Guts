using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Guts.Business.Dtos.Gamification;
using Guts.Business.Services;
using Guts.Domain.RoleAggregate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Guts.Api.Controllers
{
    [Authorize]
    [Produces("application/json")]
    [Route("api/courses/{courseId}/gamification")]
    public class GamificationController(IGamificationService gamificationService) : ControllerBase
    {
        [HttpGet("profile")]
        [ProducesResponseType(typeof(StudentGamificationProfileDto), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetProfile(int courseId, [FromQuery] int? periodId = null)
        {
            int userId = GetUserId();
            var profile = await gamificationService.GetStudentCourseProfileAsync(userId, courseId, periodId);
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
        public async Task<IActionResult> GetWeeklySprint(int courseId, [FromQuery] int? periodId = null)
        {
            int userId = GetUserId();
            var leaderboard = await gamificationService.GetWeeklySprintLeaderboardAsync(courseId, userId, periodId);
            return Ok(leaderboard);
        }

        [HttpGet("leaderboard/hall-of-fame")]
        [ProducesResponseType(typeof(IReadOnlyList<LeaderboardEntryDto>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetHallOfFame(int courseId, [FromQuery] int? periodId = null)
        {
            int userId = GetUserId();
            var leaderboard = await gamificationService.GetHallOfFameLeaderboardAsync(courseId, userId, periodId);
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
    }
}
