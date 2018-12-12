using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using Guts.Api.Models.Converters;
using Guts.Business.Services;
using Guts.Data.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Guts.Api.Controllers
{
    [Produces("application/json")]
    [Route("api/exercises")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class ExerciseController : ControllerBase
    {
        private readonly IAssignmentService _assignmentService;
        private readonly IExerciseRepository _exerciseRepository;
        private readonly IExerciseConverter _exerciseConverter;

        public ExerciseController(IAssignmentService assignmentService, 
            IExerciseRepository exerciseRepository, 
            IExerciseConverter exerciseConverter)
        {
            _assignmentService = assignmentService;
            _exerciseRepository = exerciseRepository;
            _exerciseConverter = exerciseConverter;
        }

        [HttpGet("{exerciseId}")]
        public async Task<IActionResult> GetExerciseResults(int exerciseId, [FromQuery] DateTime? date)
        {
            return await GetExerciseResultsForUser(exerciseId, GetUserId(), date);
        }

        [HttpGet("{exerciseId}/foruser/{userId}")]
        public async Task<IActionResult> GetExerciseResultsForUser(int exerciseId, int userId, [FromQuery] DateTime? date)
        {
            if (IsStudent())
            {
                //students can only see their own exercise results
                if(!IsOwnUserId(userId)) return Forbid();
            }
            else if (!IsLector())
            {
                return Forbid();
            }

            var exercise = await _exerciseRepository.GetSingleWithTestsAndCourseAsync(exerciseId);

            var dateUtc = date?.ToUniversalTime();

            var testRunInfo = await _assignmentService.GetUserTestRunInfoForExercise(exerciseId, userId, dateUtc);

            var results = await _assignmentService.GetResultsForUserAsync(exerciseId, userId, dateUtc);

            var model = _exerciseConverter.ToExerciseDetailModel(exercise, results.TestResults, testRunInfo);

            return Ok(model);
        }

        [HttpGet("{exerciseId}/getsourcecodezip")]
        public async Task<IActionResult> DownloadSourceCodesAsZip(int exerciseId)
        {
            if (!IsLector())
            {
                return Forbid();
            }

            var sourceCodes = await _assignmentService.GetAllSourceCodes(exerciseId);
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
                    FileDownloadName = $"Exercise_{exerciseId}_sources.zip"
                };
                return result;
            }
        }
    }
}