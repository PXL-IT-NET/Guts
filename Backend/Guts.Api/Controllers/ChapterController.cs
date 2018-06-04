using System.Net;
using System.Threading.Tasks;
using Guts.Api.Models;
using Guts.Api.Models.Converters;
using Guts.Business.Services;
using Guts.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Guts.Api.Controllers
{
    [Produces("application/json")]
    [Route("api/courses/{courseId}/chapters")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class ChapterController : ControllerBase
    {
        private readonly IChapterService _chapterService;
        private readonly IChapterConverter _chapterConverter;

        public ChapterController(IChapterService chapterService, IChapterConverter chapterConverter)
        {
            _chapterService = chapterService;
            _chapterConverter = chapterConverter;
        }

        /// <summary>
        /// Retrieves an overview of the testresults for a chapter of a course (for the current period).
        /// The overview contains testresults for the authorized user and the average results of all users.
        /// </summary>
        /// <param name="courseId">Identifier of the course in the database.</param>
        /// <param name="chapterNumber">Sequence number of the chapter</param>
        [HttpGet("{chapterNumber}")]
        [ProducesResponseType(typeof(ChapterContentsModel), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> GetChapterContents(int courseId, int chapterNumber)
        {
            if (courseId < 1 || chapterNumber < 1)
            {
                return BadRequest();
            }

            try
            {
                var chapter = await _chapterService.LoadChapterWithTestsAsync(courseId, chapterNumber);
                var userExerciseResults = await _chapterService.GetResultsForUserAsync(chapter.Id, GetUserId());
                var averageExerciseResults = await _chapterService.GetAverageResultsAsync(chapter.Id);
                var model = _chapterConverter.ToChapterContentsModel(chapter, userExerciseResults, averageExerciseResults);
                return Ok(model);
            }
            catch (DataNotFoundException)
            {
                return NotFound();
            }
        }
    }
}