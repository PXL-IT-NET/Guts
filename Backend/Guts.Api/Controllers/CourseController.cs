using System.Threading.Tasks;
using Guts.Api.Models.Converters;
using Guts.Business.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Guts.Api.Controllers
{
    [Produces("application/json")]
    [Route("api/courses")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class CourseController : ControllerBase
    {
        private readonly ICourseService _courseService;
        private readonly IChapterService _chapterService;
        private readonly ICourseConverter _courseConverter;

        public CourseController(ICourseService courseService, 
            IChapterService chapterService,
            ICourseConverter courseConverter)
        {
            _courseService = courseService;
            _chapterService = chapterService;
            _courseConverter = courseConverter;
        }

        [HttpGet]
        public async Task<IActionResult> GetCourses()
        {
            return Ok(await _courseService.GetAllCoursesAsync());
        }

        [HttpGet("{courseId}")]
        public async Task<IActionResult> GetCourseContents(int courseId)
        {
            if (courseId < 1)
            {
                return BadRequest();
            }

            var course = await _courseService.GetCourseByIdAsync(courseId);
            var chapters = await _chapterService.GetChaptersOfCourseAsync(courseId);
            var model = _courseConverter.ToCourseContentsModel(course, chapters);

            return Ok(model);
        }
    }
}