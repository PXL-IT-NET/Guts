using Guts.Api.Models;
using Guts.Api.Models.Converters;
using Guts.Business;
using Guts.Business.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Guts.Domain.CourseAggregate;
using Guts.Domain.TopicAggregate.ChapterAggregate;
using Guts.Domain.TopicAggregate.ProjectAggregate;

namespace Guts.Api.Controllers
{
    [Produces("application/json")]
    [Route("api/courses")]
    public class CourseController(
        ICourseService courseService,
        IChapterService chapterService,
        IProjectService projectService,
        ICourseConverter courseConverter)
        : ControllerBase
    {
        /// <summary>
        /// Retrieves an overview of all the courses 
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(IList<Course>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> GetCourses()
        {
            return Ok(await courseService.GetAllCoursesAsync());
        }

        /// <summary>
        /// Retrieves information about a course (for the current period).
        /// This includes a list of chapters.
        /// </summary>
        /// <param name="courseId">Identifier of the course in the database.</param>
        /// <param name="periodId">Optional period identifier. If provided data from a specific period will be returned.</param>
        [HttpGet("{courseId}")]
        [ProducesResponseType(typeof(CourseContentsModel), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> GetCourseContents(int courseId, [FromQuery] int? periodId = null)
        {
            if (courseId < 1)
            {
                return BadRequest();
            }

            var course = await courseService.GetCourseByIdAsync(courseId);
            IReadOnlyList<Chapter> chapters = await chapterService.GetChaptersOfCourseAsync(courseId, periodId);
            IReadOnlyList<IProject> projects = await projectService.GetProjectsOfCourseAsync(courseId, periodId);
            var model = courseConverter.ToCourseContentsModel(course, chapters, projects);

            return Ok(model);
        }

        /// <summary>
        /// Retrieves statistics about student results for a course (for the given or current period).
        /// </summary>
        /// <param name="courseId">Identifier of the course in the database.</param>
        /// <param name="periodId">Optional period identifier. If provided data from a specific period will be returned.</param>
        [HttpGet("{courseId}/statistics")]
        [Authorize(Policy = ApiConstants.LectorsOnlyPolicy)]
        [ProducesResponseType(typeof(CourseStatisticsModel), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetCourseStatistics(int courseId, [FromQuery] int? periodId = null)
        {
            if (courseId < 1)
            {
                return BadRequest();
            }

            try
            {
                var statsDto = await courseService.GetCourseStatisticsAsync(courseId, periodId);
                var model = courseConverter.ToCourseStatisticsModel(statsDto);
                return Ok(model);
            }
            catch (DataNotFoundException)
            {
                return NotFound();
            }
        }
    }
}
