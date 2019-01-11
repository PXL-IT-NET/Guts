using CsvHelper;
using Guts.Api.Models;
using Guts.Api.Models.Converters;
using Guts.Business.Services;
using Guts.Data.Repositories;
using Guts.Domain;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CsvHelper.Configuration;

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
        private readonly IUserRepository _userRepository;
        private readonly IAssignmentService _assignmentService;
        private readonly ICourseConverter _courseConverter;

        public CourseController(ICourseService courseService,
            IChapterService chapterService,
            IProjectService projectService,
            IUserRepository userRepository,
            IAssignmentService assignmentService,
            ICourseConverter courseConverter)
        {
            _courseService = courseService;
            _chapterService = chapterService;
            _projectService = projectService;
            _userRepository = userRepository;
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
            var projects = await _projectService.GetProjectsOfCourseAsync(courseId);
            var model = _courseConverter.ToCourseContentsModel(course, chapters, projects);

            return Ok(model);
        }

        [HttpPost("{courseId}/assignmentscores")]
        public async Task<IActionResult> DownloadCourseAssignmentScores(int courseId, [FromBody] ScoreOptions input)
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
                var chapter = await _chapterService.LoadChapterAsync(courseId, chapterScoreOptions.ChapterCode);
                allUsers = allUsers.Union(await _userRepository.GetUsersOfTopicAsync(chapter.Id), new DomainOjbectEqualityComparer<User>()).ToList();
            }
            allUsers = allUsers.OrderBy(u => u.LastName).ThenBy(u => u.FirstName).ToList();

            //preload the chapters
            var chapterDictionary = new Dictionary<string, Chapter>();
            foreach (var chapterScoreOptions in input.ChapterScoreOptions)
            {
                chapterDictionary[chapterScoreOptions.ChapterCode] = await _chapterService.LoadChapterWithTestsAsync(courseId, chapterScoreOptions.ChapterCode);
            }

            var results = new List<dynamic>();
            foreach (var user in allUsers)
            {
                var result = new ExpandoObject();
                result.TryAdd("LastName", user.LastName);
                result.TryAdd("FirstName", user.FirstName);

                double total = 0.0;
                double totalMaximum = 0.0;

                foreach (var chapterScoreOptions in input.ChapterScoreOptions)
                {
                    var chapter = chapterDictionary[chapterScoreOptions.ChapterCode];

                    foreach (var assignmentScoreOptions in chapterScoreOptions.AssignmentScoreOptions)
                    {
                        var assignment = chapter.Assignments.FirstOrDefault(e => e.Code == assignmentScoreOptions.AssignmentCode);
                        if (assignment == null) continue;

                        var numberOfTests = assignment.Tests.Count;

                        var resultDto = await _assignmentService.GetResultsForUserAsync(assignment.Id, user.Id, chapterScoreOptions.Date);

                        var numberOfPassedTests = resultDto?.TestResults?.Count(r => r.Passed) ?? 0;

                        double scorePerTest = assignmentScoreOptions.MaximumScore /
                                              (numberOfTests - assignmentScoreOptions.MinimumNumberOfGreenTestsThreshold);

                        double score =
                            Math.Max(numberOfPassedTests - assignmentScoreOptions.MinimumNumberOfGreenTestsThreshold, 0) *
                            scorePerTest;

                        total += score;
                        totalMaximum += assignmentScoreOptions.MaximumScore;

                        result.TryAdd($"{chapter.Code}.{assignmentScoreOptions.AssignmentCode}_NbrPassed({numberOfTests})", numberOfPassedTests);
                        result.TryAdd($"{chapter.Code}.{assignmentScoreOptions.AssignmentCode}_Score({assignmentScoreOptions.MaximumScore})", score);
                    }
                }

                result.TryAdd($"Total({totalMaximum})", total);

                results.Add(result);
            }

            var memoryStream = new MemoryStream();
            var streamWriter = new StreamWriter(memoryStream);
            var config = new Configuration
            {
                CultureInfo = new CultureInfo("nl-BE"),
            };
            var csv = new CsvWriter(streamWriter, config);
            csv.WriteRecords(results);
            streamWriter.Flush();
            memoryStream.Position = 0;
            return File(memoryStream, "text/csv", "AssignmentScores.csv");
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
        public string ChapterCode { get; set; }
        public IList<AssignmentScoreOptions> AssignmentScoreOptions { get; set; }
        public DateTime Date { get; set; }
    }

    public class AssignmentScoreOptions
    {
        public string AssignmentCode { get; set; }
        public double MaximumScore { get; set; }
        public int MinimumNumberOfGreenTestsThreshold { get; set; }
    }

}