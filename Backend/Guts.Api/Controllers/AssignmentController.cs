using Guts.Api.Models.Converters;
using Guts.Business.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
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

        public AssignmentController(IAssignmentService assignmentService,
            IAssignmentRepository assignmentRepository,
            IAssignmentConverter assignmentConverter, 
            IProjectTeamRepository projectTeamRepository)
        {
            _assignmentService = assignmentService;
            _assignmentRepository = assignmentRepository;
            _assignmentConverter = assignmentConverter;
            _projectTeamRepository = projectTeamRepository;
        }

        [HttpGet("{assignmentId}")]
        public async Task<IActionResult> GetAssignmentResults(int assignmentId, [FromQuery] DateTime? date)
        {
            return await GetAssignmentResultsForUser(assignmentId, GetUserId(), date);
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

            var model = _assignmentConverter.ToAssignmentDetailModel(assignment, results.TestResults, testRunInfo);

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

            var model = _assignmentConverter.ToAssignmentDetailModel(assignment, results.TestResults, testRunInfo);

            return Ok(model);
        }

        [HttpGet("{assignmentId}/getsourcecodezip")]
        public async Task<IActionResult> DownloadSourceCodesAsZip(int assignmentId)
        {
            if (!IsLector())
            {
                return Forbid();
            }

            var sourceCodes = await _assignmentService.GetAllSourceCodes(assignmentId);
            using (var memoryStream = new MemoryStream())
            {
                using (var zipArchive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
                {
                    foreach (var sourceCode in sourceCodes)
                    {
                        var entry = zipArchive.CreateEntry($@"{sourceCode.UserFullName}\source.txt");
                        using (StreamWriter writer = new StreamWriter(entry.Open()))
                        {
                            await writer.WriteAsync(sourceCode.Source);
                        }
                    }
                }

                memoryStream.Position = 0;

                var result = new FileContentResult(memoryStream.ToArray(), "application/zip")
                {
                    FileDownloadName = $"Assignment_{assignmentId}_sources.zip"
                };
                return result;
            }
        }
    }
}