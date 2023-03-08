using Guts.Api.Models.Converters;
using Guts.Business.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Guts.Api.Models;
using Guts.Business.Dtos;
using Guts.Business.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace Guts.Api.Controllers
{
    [Produces("application/json")]
    [Route("api/assignments")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class AssignmentController : ControllerBase
    {
        private readonly IAssignmentService _assignmentService;
        private readonly IAssignmentRepository _assignmentRepository;
        private readonly IAssignmentConverter _assignmentConverter;
        private readonly IProjectTeamRepository _projectTeamRepository;
        private readonly ITopicService _topicService;
        private readonly ISolutionFileRepository _solutionFileRepository;
        private readonly ISolutionFileService _solutionFileService;
        private readonly IMapper _mapper;

        public AssignmentController(IAssignmentService assignmentService,
            IAssignmentRepository assignmentRepository,
            IAssignmentConverter assignmentConverter,
            IProjectTeamRepository projectTeamRepository,
            ITopicService topicService,
            ISolutionFileRepository solutionFileRepository,
            ISolutionFileService solutionFileService,
            IMapper mapper)
        {
            _assignmentService = assignmentService;
            _assignmentRepository = assignmentRepository;
            _assignmentConverter = assignmentConverter;
            _projectTeamRepository = projectTeamRepository;
            _topicService = topicService;
            _solutionFileRepository = solutionFileRepository;
            _solutionFileService = solutionFileService;
            _mapper = mapper;
        }

        [HttpGet("{assignmentId}")]
        public async Task<IActionResult> GetAssignmentResults(int assignmentId, [FromQuery] DateTime? date)
        {
            return await GetAssignmentResultsForUser(assignmentId, GetUserId(), date);
        }

        [HttpGet("ofcourse/{courseId}")]
        public async Task<IActionResult> GetAssignmentsOfCourse(int courseId)
        {
            //TODO: write tests
            var topics = await _topicService.GetTopicsByCourseWithAssignmentsAndTestsAsync(courseId);
            var models = topics.SelectMany(t => t.Assignments).Select(a => _mapper.Map<TopicAssignmentModel>(a));
            return Ok(models);
        }

        [HttpGet("{assignmentId}/foruser/{userId}")]
        public async Task<IActionResult> GetAssignmentResultsForUser(int assignmentId, int userId, [FromQuery] DateTime? date)
        {
            if (IsStudent())
            {
                //students can only see their own assignment results
                if (!IsOwnUserId(userId)) return Forbid();
            }
            else if (!IsLector())
            {
                return Forbid();
            }

            var assignment = await _assignmentRepository.GetSingleWithTestsAndCourseAsync(assignmentId);

            var dateUtc = date?.ToUniversalTime();

            var testRunInfo = await _assignmentService.GetUserTestRunInfoForAssignment(assignmentId, userId, dateUtc);

            var results = await _assignmentService.GetResultsForUserAsync(assignmentId, userId, dateUtc);

            var solutionFiles =
                await _solutionFileRepository.GetAllLatestOfAssignmentForUserAsync(assignmentId, userId, dateUtc);

            var model = _assignmentConverter.ToAssignmentDetailModel(assignment, testRunInfo, results.TestResults, solutionFiles);

            return Ok(model);
        }

        [HttpGet("{assignmentId}/forteam/{teamId}")]
        public async Task<IActionResult> GetAssignmentResultsForTeam(int assignmentId, int teamId, [FromQuery] DateTime? date)
        {
            var team = await _projectTeamRepository.LoadByIdAsync(teamId);
            if (IsStudent())
            {
                if (team.TeamUsers.All(tu => tu.UserId != GetUserId()))
                {
                    return Forbid();
                }
            }
            else if (!IsLector())
            {
                return Forbid();
            }

            var assignment = await _assignmentRepository.GetSingleWithTestsAndCourseAsync(assignmentId);
            var dateUtc = date?.ToUniversalTime();

            var testRunInfo = await _assignmentService.GetTeamTestRunInfoForAssignment(assignmentId, teamId, dateUtc);

            var results = await _assignmentService.GetResultsForTeamAsync(assignmentId, teamId, dateUtc);

            var solutionFiles =
                await _solutionFileRepository.GetAllLatestOfAssignmentForTeamAsync(assignmentId, teamId, dateUtc);

            var model = _assignmentConverter.ToAssignmentDetailModel(assignment, testRunInfo, results.TestResults, solutionFiles);

            return Ok(model);
        }

        [HttpGet("{assignmentId}/getsourcecodezip")]
        public async Task<IActionResult> DownloadSourceCodesAsZip(int assignmentId)
        {
            if (!IsLector())
            {
                return Forbid();
            }

            IReadOnlyList<SolutionDto> solutions = await _assignmentService.GetAllSolutions(assignmentId);
            byte[] bytes = await _solutionFileService.CreateZipFromFiles(solutions);
            var result = new FileContentResult(bytes, "application/zip")
            {
                FileDownloadName = $"Assignment_{assignmentId}_sources.zip"
            };
            return result;
        }
    }
}