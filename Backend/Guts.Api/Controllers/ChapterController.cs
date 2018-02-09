using System.Threading.Tasks;
using Guts.Api.Models.Converters;
using Guts.Business;
using Guts.Business.Services;
using Guts.Data;
using Microsoft.AspNetCore.Mvc;

namespace Guts.Api.Controllers
{
    [Produces("application/json")]
    [Route("api/courses/{courseId}/chapters")]
    public class ChapterController : ControllerBase
    {
        private readonly IChapterService _chapterService;
        private readonly IChapterConverter _chapterConverter;

        public ChapterController(IChapterService chapterService, IChapterConverter chapterConverter)
        {
            _chapterService = chapterService;
            _chapterConverter = chapterConverter;
        }

        [HttpGet("{chapterNumber}")]
        public async Task<IActionResult> GetChapterContents(int courseId, int chapterNumber)
        {
            if (courseId < 1 || chapterNumber < 1)
            {
                return BadRequest();
            }

            try
            {
                var chapter = await _chapterService.LoadChapterWithTestsAsync(courseId, chapterNumber);
                var model = _chapterConverter.ToChapterContentsModel(chapter);
                return Ok(model);
            }
            catch (DataNotFoundException)
            {
                return NotFound();
            }
        }
    }
}