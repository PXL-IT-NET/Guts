using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Guts.Api.Models;
using Guts.Api.Models.Converters;
using Guts.Business.Services;
using Guts.Data;
using Guts.Data.Repositories;
using Guts.Domain;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace Guts.Api.Controllers
{
    [Produces("application/json")]
    [Route("api/courses/{courseId}/chapters")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class ChapterController : ControllerBase
    {
        private readonly IChapterService _chapterService;
        private readonly IChapterRepository _chapterRepository;
        private readonly IChapterConverter _chapterConverter;
        private readonly UserManager<User> _userManager;
        private readonly IMemoryCache _memoryCache;
        public const int CacheTimeInSeconds = 300;

        public ChapterController(IChapterService chapterService,
            IChapterRepository chapterRepository,
            IChapterConverter chapterConverter,
            UserManager<User> userManager, 
            IMemoryCache memoryCache)
        {
            _chapterService = chapterService;
            _chapterRepository = chapterRepository;
            _chapterConverter = chapterConverter;
            _userManager = userManager;
            _memoryCache = memoryCache;
        }

        /// <summary>
        /// Retrieves an overview of the testresults for a chapter of a course (for the current period).
        /// The overview contains testresults for the authorized user and the average results of all users.
        /// </summary>
        /// <param name="courseId">Identifier of the course in the database.</param>
        /// <param name="chapterNumber">Sequence number of the chapter</param>
        [HttpGet("{chapterNumber}")]
        [ProducesResponseType(typeof(ChapterDetailModel), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> GetChapterDetails(int courseId, int chapterNumber)
        {
            if (courseId < 1 || chapterNumber < 1)
            {
                return BadRequest();
            }

            try
            {
                var chapter = await _chapterService.LoadChapterAsync(courseId, chapterNumber);

                List<User> chapterUsers = new List<User>();
                if (IsLector())
                {
                    chapterUsers.AddRange(await _chapterRepository.GetUsersOfChapterAsync(chapter.Id));
                }

                if (!chapterUsers.Any()) //add the own user
                {
                    chapterUsers.Add(await _userManager.GetUserAsync(User));
                }

                var model = _chapterConverter.ToChapterDetailModel(chapter, chapterUsers);
                return Ok(model);
            }
            catch (DataNotFoundException)
            {
                return NotFound();
            }
        }

        /// <summary>
        /// Retrieves an overview of the testresults for a chapter of a course (for the current period).
        /// The overview contains testresults for the authorized user and the average results of all users.
        /// </summary>
        /// <param name="courseId">Identifier of the course in the database.</param>
        /// <param name="chapterNumber">Sequence number of the chapter.</param>
        /// <param name="userId">Identifier of the user for which the summary should be retrieved.</param>
        /// <param name="date">Optional date paramter. If provided the status of the summary on that date will be returned.</param>
        [HttpGet("{chapterNumber}/users/{userId}/summary")]
        [ProducesResponseType(typeof(ChapterSummaryModel), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> GetChapterSummary(int courseId, int chapterNumber, int userId, [FromQuery] DateTime? date)
        {
            if (courseId < 1 || chapterNumber < 1 || userId < 1)
            {
                return BadRequest();
            }

            if (IsStudent())
            {
                //students can only see their own chapter summary
                if (!IsOwnUserId(userId)) return Forbid();
            }
            else if (!IsLector())
            {
                return Forbid();
            }

            var dateUtc = date?.ToUniversalTime();

            try
            {
                var chapter = await _chapterService.LoadChapterWithTestsAsync(courseId, chapterNumber);
                var userExerciseResults = await _chapterService.GetResultsForUserAsync(chapter.Id, userId, dateUtc);
                var model = _chapterConverter.ToChapterSummaryModel(chapter, userExerciseResults);
                return Ok(model);
            }
            catch (DataNotFoundException)
            {
                return NotFound();
            }
        }

        /// <summary>
        /// Retrieves an overview of the exercise statistics for a chapter of a course (for the current period).
        /// </summary>
        /// <param name="courseId">Identifier of the course in the database.</param>
        /// <param name="chapterNumber">Sequence number of the chapter.</param>
        /// <param name="date">Optional date paramter. If provided the status of the summary on that date will be returned.</param>
        [HttpGet("{chapterNumber}/statistics")]
        [ProducesResponseType(typeof(ChapterSummaryModel), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> GetChapterStatistics(int courseId, int chapterNumber, [FromQuery] DateTime? date)
        {
            if (courseId < 1 || chapterNumber < 1)
            {
                return BadRequest();
            }

            var dateUtc = date?.ToUniversalTime();
            bool useCache = !(dateUtc.HasValue && DateTime.UtcNow.Subtract(dateUtc.Value).TotalSeconds > CacheTimeInSeconds);

            var cacheKey = $"GetChapterStatistics-{courseId}-{chapterNumber}";
            if (!useCache || !_memoryCache.TryGetValue(cacheKey, out ChapterStatisticsModel model))
            {
                try
                {
                    var chapter = await _chapterService.LoadChapterAsync(courseId, chapterNumber);
                    var chapterStatistics = await _chapterService.GetChapterStatisticsAsync(chapter.Id, dateUtc);
                    model = _chapterConverter.ToChapterStatisticsModel(chapter, chapterStatistics);

                    if (useCache)
                    {
                        _memoryCache.Set(cacheKey, model, DateTime.Now.AddSeconds(CacheTimeInSeconds));
                    }
                }
                catch (DataNotFoundException)
                {
                    return NotFound();
                }
            }

            return Ok(model);
        }
    }
}