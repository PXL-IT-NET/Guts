using Guts.Api.Models;
using Guts.Api.Models.Converters;
using Guts.Business.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Guts.Domain.CourseAggregate;

namespace Guts.Api.Controllers
{
    [Produces("application/json")]
    [Route("api/courses")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class CourseController : ControllerBase
    {
        private readonly ICourseService _courseService;
        private readonly IChapterService _chapterService;
        private readonly IProjectService _projectService;
        private readonly ICourseConverter _courseConverter;

        public CourseController(ICourseService courseService,
            IChapterService chapterService,
            IProjectService projectService,
            ICourseConverter courseConverter)
        {
            _courseService = courseService;
            _chapterService = chapterService;
            _projectService = projectService;
            _courseConverter = courseConverter;
        }

        /// <summary>
        /// Retrieves an overview of all the courses 
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(IList<Course>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> GetCourses()
        {
            return Ok(await _courseService.GetAllCoursesAsync());
        }

        /// <summary>
        /// Retrieves information about a course (for the current period).
        /// This includes a list of chapters.
        /// </summary>
        /// <param name="courseId">Identifier of the course in the database.</param>
        [HttpGet("{courseId}")]
        [ProducesResponseType(typeof(CourseContentsModel), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> GetCourseContents(int courseId)
        {
            if (courseId < 1)
            {
                return BadRequest();
            }

            var course = await _courseService.GetCourseByIdAsync(courseId);
            var chapters = await _chapterService.GetChaptersOfCourseAsync(courseId);
            var projects = await _projectService.GetProjectsOfCourseAsync(courseId);
            var model = _courseConverter.ToCourseContentsModel(course, chapters, projects);

            return Ok(model);
        }
    }
}