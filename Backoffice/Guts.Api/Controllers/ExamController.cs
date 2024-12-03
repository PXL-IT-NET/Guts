using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AutoMapper;
using CsvHelper;
using Guts.Api.Models;
using Guts.Api.Models.ExamModels;
using Guts.Business;
using Guts.Business.Dtos;
using Guts.Business.Services.Exam;
using Guts.Common;
using Guts.Common.Extensions;
using Guts.Domain.ExamAggregate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Guts.Api.Controllers
{
    [Produces("application/json")]
    [Route("api/exams")]
    [Authorize(Policy = ApiConstants.LectorsOnlyPolicy)]
    public class ExamController : ControllerBase
    {
        private readonly IExamService _examService;
        private readonly IMapper _mapper;

        public ExamController(IExamService examService, 
            IMapper mapper)
        {
            _examService = examService;
            _mapper = mapper;
        }

        /// <summary>
        /// Retrieves all exams of a course.
        /// </summary>
        /// <param name="courseId">Identifier of the course</param>
        /// <param name="periodId">Optional period identifier. If provided data from a specific period will be returned.</param>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<ExamOutputModel>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<ActionResult> GetExams([FromQuery] int courseId, [FromQuery] int? periodId = null)
        {
            if (courseId <= 0)
            {
                return BadRequest(ErrorModel.FromString("Invalid course Id."));
            }
            var exams = await _examService.GetExamsAsync(courseId, periodId);
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
        /// <param name="id">Identifier of the exam</param>
        /// <param name="examPartId">Identifier of the exam part</param>
        [HttpGet("{id}/parts/{examPartId}")]
        [ProducesResponseType(typeof(ExamPartOutputModel), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<ActionResult> GetExamPart(int id, int examPartId)
        {
            try
            {
                var exam = await _examService.GetExamAsync(id);
                var examPart = exam.Parts.FirstOrDefault(part => part.Id == examPartId);
                if(examPart == null) throw new DataNotFoundException();
                var model = _mapper.Map<ExamPartOutputModel>(examPart);
                return Ok(model);
            }
            catch (DataNotFoundException)
            {
                return NotFound();
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

        /// <summary>
        /// Deletes an exam part from an existing exam
        /// </summary>
        [HttpDelete("{id}/parts/{examPartId}")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> DeleteExamPart(int id, int examPartId)
        {
            try
            {
                var exam = await _examService.GetExamAsync(id);
                await _examService.DeleteExamPartAsync(exam, examPartId);
                return Ok();
            }
            catch (ContractException ex)
            {
                return BadRequest(ErrorModel.FromException(ex));
            }
        }

        [HttpGet("{id}/downloadscores")]
        public async Task<IActionResult> DownloadExamScores(int id)
        {
            IExam exam;
            try
            {
                exam = await _examService.GetExamAsync(id);
            }
            catch (DataNotFoundException)
            {
                return NotFound();
            }

            var fileDownloadName = $"{exam.Name.ToValidFilePath()}.csv";

            IEnumerable<dynamic> examScoreCsvRecords = await _examService.CalculateExamScoresForCsvAsync(id);
            
            var memoryStream = new MemoryStream();
            var streamWriter = new StreamWriter(memoryStream);
            var csv = new CsvWriter(streamWriter, new CultureInfo("nl-BE"));
            csv.WriteRecords(examScoreCsvRecords);
            streamWriter.Flush();
            memoryStream.Position = 0;
            return File(memoryStream, "text/csv", fileDownloadName);
        }
    }
}