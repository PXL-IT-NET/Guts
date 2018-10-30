using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CsvHelper;
using Guts.Api.Models;
using Guts.Api.Models.Converters;
using Guts.Business.Services;
using Guts.Data.Repositories;
using Guts.Domain;
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
        private readonly IChapterRepository _chapterRepository;
        private readonly IAssignmentService _assignmentService;
        private readonly ICourseConverter _courseConverter;

        public CourseController(ICourseService courseService, 
            IChapterService chapterService,
            IChapterRepository chapterRepository,
            IAssignmentService assignmentService,
            ICourseConverter courseConverter)
        {
            _courseService = courseService;
            _chapterService = chapterService;
            _chapterRepository = chapterRepository;
            _assignmentService = assignmentService;
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
            var model = _courseConverter.ToCourseContentsModel(course, chapters);

            return Ok(model);
        }

        [HttpPost("{courseId}/exercisescores")]
        public async Task<IActionResult> DownloadCourseExerciseScores(int courseId, [FromBody] ScoreOptions input)
        {
            //TODO: clean up this quick an dirty (non-performant) implementation.
            if (!IsLector())
            {
                return Forbid();
            }

            //Get (and sort) all users
            var allUsers = new List<User>();
            foreach (var chapterScoreOptions in input.ChapterScoreOptions)
            {
                var chapter = await _chapterService.LoadChapterAsync(courseId, chapterScoreOptions.ChapterNumber);
                allUsers = allUsers.Union(await _chapterRepository.GetUsersOfChapterAsync(chapter.Id), new DomainOjbectEqualityComparer<User>()).ToList();
            }
            allUsers = allUsers.OrderBy(u => u.LastName).ThenBy(u => u.FirstName).ToList();

            //preload the chapters
            var chapterDictionary = new Dictionary<int, Chapter>();
            foreach (var chapterScoreOptions in input.ChapterScoreOptions)
            {
                chapterDictionary[chapterScoreOptions.ChapterNumber] = await _chapterService.LoadChapterWithTestsAsync(courseId, chapterScoreOptions.ChapterNumber);
            }

            var results = new List<dynamic>();
            foreach (var user in allUsers)
            {
                var result = new ExpandoObject();
                result.TryAdd("LastName", user.LastName);
                result.TryAdd("FirstName", user.FirstName);

                foreach (var chapterScoreOptions in input.ChapterScoreOptions)
                {
                    var chapter = chapterDictionary[chapterScoreOptions.ChapterNumber];
                    foreach (var exerciseScoreOptions in chapterScoreOptions.ExerciseScoreOptions)
                    {
                        var exercise = chapter.Exercises.FirstOrDefault(e => e.Code == exerciseScoreOptions.ExerciseCode);
                        if(exercise == null) continue;

                        var numberOfTests = exercise.Tests.Count;

                        var resultDto = await _assignmentService.GetResultsForUserAsync(exercise.Id, user.Id, chapterScoreOptions.Date);

                        var numberOfPassedTests = resultDto?.TestResults?.Count(r => r.Passed) ?? 0;

                        double scorePerTest = exerciseScoreOptions.MaximumScore /
                                              (numberOfTests - exerciseScoreOptions.MinimumNumberOfGreenTestsThreshold);

                        double score =
                            Math.Max(numberOfPassedTests - exerciseScoreOptions.MinimumNumberOfGreenTestsThreshold, 0) *
                            scorePerTest;

                        result.TryAdd($"{chapter.Number}.{exerciseScoreOptions.ExerciseCode}_NbrPassed",numberOfPassedTests);
                        result.TryAdd($"{chapter.Number}.{exerciseScoreOptions.ExerciseCode}_Score", score);
                    }
                }
                results.Add(result);
            }


            using (var memoryStream = new MemoryStream())
            {
                using (var streamWriter = new StreamWriter(memoryStream))
                {
                    var csv = new CsvWriter(streamWriter);

                    csv.WriteRecords(results);
                    csv.Flush();

                    memoryStream.Position = 0;
                    var result = new FileContentResult(memoryStream.ToArray(), "text/csv")
                    {
                        FileDownloadName = "ExerciseScores.csv"
                    };
                    return result;
                }
            }
        }
    }

    public class DomainOjbectEqualityComparer<T> : IEqualityComparer<T> where T : IDomainObject
    {
        public bool Equals(T x, T y)
        {
            return x.Id == y.Id;
        }

        public int GetHashCode(T obj)
        {
            return obj.Id.GetHashCode();
        }
    }

    public class ScoreOptions
    {
        public IList<ChapterScoreOptions> ChapterScoreOptions { get; set; }
    }

    public class ChapterScoreOptions
    {
        public int ChapterNumber { get; set; }
        public IList<ExerciseScoreOptions> ExerciseScoreOptions { get; set; }
        public DateTime Date { get; set; }
    }

    public class ExerciseScoreOptions
    {
        public string ExerciseCode { get; set; }
        public double MaximumScore { get; set; }
        public int MinimumNumberOfGreenTestsThreshold { get; set; }
    }
}