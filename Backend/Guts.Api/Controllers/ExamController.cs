using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AutoMapper;
using Guts.Api.Models;
using Guts.Business;
using Guts.Business.Dtos;
using Guts.Business.Services;
using Guts.Common;
using Guts.Domain.RoleAggregate;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Guts.Api.Controllers
{
    [Produces("application/json")]
    [Route("api/exams")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = Role.Constants.Lector)]
    public class ExamController : ControllerBase
    {
        private readonly IExamService _examService;
        private readonly IMapper _mapper;

        public ExamController(IExamService examService, IMapper mapper)
        {
            _examService = examService;
            _mapper = mapper;
        }

        /// <summary>
        /// Retrieves all exams.
        /// Optionally filtered by course.
        /// </summary>
        /// <param name="courseId"></param>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<ExamOutputModel>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<ActionResult> GetExams([FromQuery] int? courseId = null)
        {
            if (courseId <= 0)
            {
                return BadRequest(ErrorModel.FromString("Invalid course Id."));
            }
            var exams = await _examService.GetExamsAsync(courseId);
            var model = exams.Select(e => _mapper.Map<ExamOutputModel>(e));
            return Ok(model);
        }

        /// <summary>
        /// Retrieves an exam.
        /// </summary>
        /// <param name="id">Identifier of the exam in the database</param>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ExamOutputModel), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<ActionResult> GetExam(int id)
        {
            try
            {
                var exam = await _examService.GetExamAsync(id);
                var model = _mapper.Map<ExamOutputModel>(exam);
                return Ok(model);
            }
            catch (DataNotFoundException)
            {
                return NotFound();
            }
        }

        /// <summary>
        /// Creates a new exam
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(ExamOutputModel), (int)HttpStatusCode.Created)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> PostExam([FromBody]ExamCreationModel model)
        {
            try
            {
                var exam = await _examService.CreateExamAsync(model.CourseId, model.Name);
                var outputModel = _mapper.Map<ExamOutputModel>(exam);
                return CreatedAtAction(nameof(GetExam), new { id = outputModel.Id }, outputModel);
            }
            catch (ContractException ex)
            {
                return BadRequest(ErrorModel.FromException(ex));
            }
        }

        /// <summary>
        /// Retrieves an exam part.
        /// </summary>
        /// <param name="id">Identifier of the exam part in the database</param>
        [HttpGet("{id}/parts/{examPartId}")]
        [ProducesResponseType(typeof(ExamOutputModel), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<ActionResult> GetExamPart(int id, int examPartId)
        {
            try
            {
                var examPart = await _examService.GetExamPartAsync(id, examPartId);
                var model = _mapper.Map<ExamPartOutputModel>(examPart);
                return Ok(model);
            }
            catch (DataNotFoundException)
            {
                return NotFound();
            }
            catch (ContractException ex)
            {
                return BadRequest(ErrorModel.FromException(ex));
            }
        }

        /// <summary>
        /// Adds a new exam part to an existing exam
        /// </summary>
        [HttpPost("{id}/parts")]
        [ProducesResponseType(typeof(ExamPartOutputModel), (int)HttpStatusCode.Created)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> PostExamPart(int id, [FromBody]ExamPartDto model)
        {
            //TODO: write tests
            try
            {
                var examPart = await _examService.CreateExamPartAsync(id, model);
                var outputModel = _mapper.Map<ExamPartOutputModel>(examPart);
                return CreatedAtAction(nameof(GetExamPart), new { id = id, examPartId = outputModel.Id }, outputModel);
            }
            catch (ContractException ex)
            {
                return BadRequest(ErrorModel.FromException(ex));
            }
        }
    }
}