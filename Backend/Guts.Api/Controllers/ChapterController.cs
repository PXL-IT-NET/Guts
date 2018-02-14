﻿using System.Threading.Tasks;
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
                var userExerciseResults = await _chapterService.GetResultsForUserAsync(chapter.Id, UserId);
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