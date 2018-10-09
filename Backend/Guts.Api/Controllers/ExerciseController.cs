using System;
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
        private readonly IExerciseService _exerciseService;
        private readonly IExerciseRepository _exerciseRepository;
        private readonly IExerciseConverter _exerciseConverter;

        public ExerciseController(IExerciseService exerciseService, 
            IExerciseRepository exerciseRepository, 
            IExerciseConverter exerciseConverter)
        {
            _exerciseService = exerciseService;
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

            var testRunInfo = await _exerciseService.GetUserTestRunInfoForExercise(exerciseId, userId, dateUtc);

            var resultDto = await _exerciseService.GetResultsForUserAsync(exerciseId, userId, dateUtc);

            var model = _exerciseConverter.ToExerciseDetailModel(exercise, resultDto, testRunInfo);

            return Ok(model);
        }
    }
}