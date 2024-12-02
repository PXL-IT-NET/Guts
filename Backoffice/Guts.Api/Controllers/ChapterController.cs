using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Guts.Api.Models;
using Guts.Api.Models.Converters;
using Guts.Business;
using Guts.Business.Repositories;
using Guts.Business.Services;
using Guts.Domain.TopicAggregate.ChapterAggregate;
using Guts.Domain.UserAggregate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace Guts.Api.Controllers
{
    [Produces("application/json")]
    [Route("api/courses/{courseId}/chapters")]
    public class ChapterController : ControllerBase
    {
        private readonly IChapterService _chapterService;
        private readonly IChapterConverter _chapterConverter;
        private readonly ITopicConverter _topicConverter;
        private readonly UserManager<User> _userManager;
        private readonly IUserRepository _userRepository;
        private readonly IMemoryCache _memoryCache;
        public const int CacheTimeInSeconds = 300;

        public ChapterController(IChapterService chapterService,
            IChapterConverter chapterConverter,
            ITopicConverter topicConverter,
            UserManager<User> userManager, 
            IUserRepository userRepository,
            IMemoryCache memoryCache)
        {
            _chapterService = chapterService;
            _chapterConverter = chapterConverter;
            _topicConverter = topicConverter;
            _userManager = userManager;
            _userRepository = userRepository;
            _memoryCache = memoryCache;
        }

        /// <summary>
        /// Retrieves the properties of a chapter of a course (for the current period).
        /// This includes the assignments of the chapter with their tests and the users that submitted testresults
        /// </summary>
        /// <param name="courseId">Identifier of the course in the database.</param>
        /// <param name="chapterCode">Sequence number of the chapter</param>
        [HttpGet("{chapterCode}")]
        [ProducesResponseType(typeof(ChapterDetailModel), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> GetChapterDetails(int courseId, string chapterCode)
        {
            if (courseId < 1 || string.IsNullOrEmpty(chapterCode))
            {
                return BadRequest();
            }

            try
            {
                Chapter chapter = await _chapterService.LoadChapterWithTestsAsync(courseId, chapterCode);

                List<User> chapterUsers = new List<User>();
                if (IsLector())
                {
                    chapterUsers.AddRange(await _userRepository.GetUsersOfTopicAsync(chapter.Id));
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

        [HttpPut("{chapterCode}")]
        [Authorize(Policy = ApiConstants.LectorsOnlyPolicy)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> UpdateChapter(int courseId, string chapterCode, [FromBody] TopicUpdateModel model)
        {
            if (courseId < 1 || string.IsNullOrEmpty(chapterCode))
            {
                return BadRequest();
            }

            await _chapterService.UpdateChapterAsync(courseId, chapterCode, null, model.Description);

            return Ok();
        }

        /// <summary>
        /// Retrieves an overview of the testresults for a chapter of a course (for the current period).
        /// The overview contains testresults for the authorized user.
        /// </summary>
        /// <param name="courseId">Identifier of the course in the database.</param>
        /// <param name="chapterCode"></param>
        /// <param name="userId">Identifier of the user for which the summary should be retrieved.</param>
        /// <param name="date">Optional date paramter. If provided the status of the summary on that date will be returned.</param>
        [HttpGet("{chapterCode}/users/{userId}/summary")]
        [ProducesResponseType(typeof(TopicSummaryModel), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> GetChapterSummary(int courseId, string chapterCode, int userId,
            [FromQuery] DateTime? date)
        {
            if (courseId < 1 || string.IsNullOrEmpty(chapterCode) || userId < 1)
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
                var chapter = await _chapterService.LoadChapterWithTestsAsync(courseId, chapterCode);
                var assignmentResults = await _chapterService.GetResultsForUserAsync(chapter, userId, dateUtc);
                var model = _topicConverter.ToTopicSummaryModel(chapter, assignmentResults);
                return Ok(model);
            }
            catch (DataNotFoundException)
            {
                return NotFound();
            }
        }

        /// <summary>
        /// Retrieves an overview of the assignment statistics for a chapter of a course (for the current period).
        /// </summary>
        /// <param name="courseId">Identifier of the course in the database.</param>
        /// <param name="chapterCode">Sequence number of the chapter.</param>
        /// <param name="date">Optional date parameter. If provided the status of the summary on that date will be returned.</param>
        [HttpGet("{chapterCode}/statistics")]
        [ProducesResponseType(typeof(TopicStatisticsModel), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> GetChapterStatistics(int courseId, string chapterCode, [FromQuery] DateTime? date)
        {
            if (courseId < 1 || string.IsNullOrEmpty(chapterCode))
            {
                return BadRequest();
            }

            var dateUtc = date?.ToUniversalTime();
            bool useCache = !(dateUtc.HasValue && DateTime.UtcNow.Subtract(dateUtc.Value).TotalSeconds > CacheTimeInSeconds);

            var cacheKey = $"GetChapterStatistics-{courseId}-{chapterCode}";
            if (!useCache || !_memoryCache.TryGetValue(cacheKey, out TopicStatisticsModel model))
            {
                try
                {
                    var chapter = await _chapterService.LoadChapterAsync(courseId, chapterCode);
                    var chapterStatistics = await _chapterService.GetChapterStatisticsAsync(chapter, dateUtc);
                    model = _topicConverter.ToTopicStatisticsModel(chapter, chapterStatistics, "Students");

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