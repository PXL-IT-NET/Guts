using Guts.Api.Models;
using Guts.Api.Models.Converters;
using Guts.Application;
using Guts.Application.Services;
using Guts.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Guts.Domain.CourseAggregate;
using Guts.Domain.TopicAggregate.ChapterAggregate;
using Guts.Domain.TopicAggregate.ProjectAggregate;

namespace Guts.Api.Controllers;

[Produces("application/json")]
[Route("api/courses")]
public class CourseController(
    ICourseService courseService,
    IChapterService chapterService,
    IProjectService projectService,
    ICourseConverter courseConverter)
    : ControllerBase
{
    /// <summary>
    /// Retrieves an overview of all the courses 
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IList<Course>), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
    public async Task<IActionResult> GetCourses()
    {
        return Ok(await courseService.GetAllCoursesAsync());
    }

    /// <summary>
    /// Retrieves information about a course (for the current period).
    /// This includes a list of chapters.
    /// </summary>
    /// <param name="courseId">Identifier of the course in the database.</param>
    /// <param name="periodId">Optional period identifier. If provided data from a specific period will be returned.</param>
    [HttpGet("{courseId}")]
    [ProducesResponseType(typeof(CourseContentsModel), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
    public async Task<IActionResult> GetCourseContents(int courseId, [FromQuery] int? periodId = null)
    {
        if (courseId < 1)
        {
            return BadRequest();
        }

        var course = await courseService.GetCourseByIdAsync(courseId);
        IReadOnlyList<Chapter> chapters = await chapterService.GetChaptersOfCourseAsync(courseId, periodId);
        IReadOnlyList<IProject> projects = await projectService.GetProjectsOfCourseAsync(courseId, periodId);
        var model = courseConverter.ToCourseContentsModel(course, chapters, projects);

        return Ok(model);
    }

    /// <summary>
    /// Retrieves statistics about student results for a course (for the given or current period).
    /// </summary>
    /// <param name="courseId">Identifier of the course in the database.</param>
    /// <param name="periodId">Optional period identifier. If provided data from a specific period will be returned.</param>
    [HttpGet("{courseId}/statistics")]
    [Authorize(Policy = ApiConstants.LectorsOnlyPolicy)]
    [ProducesResponseType(typeof(CourseStatisticsModel), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<IActionResult> GetCourseStatistics(int courseId, [FromQuery] int? periodId = null)
    {
        if (courseId < 1)
        {
            return BadRequest();
        }

        try
        {
            var statsDto = await courseService.GetCourseStatisticsAsync(courseId, periodId);
            var model = courseConverter.ToCourseStatisticsModel(statsDto);
            return Ok(model);
        }
        catch (DataNotFoundException)
        {
            return NotFound();
        }
    }

    /// <summary>
    /// Creates a new course.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = ApiConstants.AdminOnlyPolicy)]
    [ProducesResponseType(typeof(Course), (int)HttpStatusCode.Created)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    public async Task<IActionResult> CreateCourse([FromBody] CourseSaveModel model)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        try
        {
            Course course = await courseService.CreateCourseAsync(model.Code, model.Name);
            return CreatedAtAction(nameof(GetCourseContents), new { courseId = course.Id }, course);
        }
        catch (ContractException ex)
        {
            return BadRequest(ErrorModel.FromException(ex));
        }
    }

    /// <summary>
    /// Updates an existing course.
    /// </summary>
    [HttpPut("{courseId}")]
    [Authorize(Policy = ApiConstants.AdminOnlyPolicy)]
    [ProducesResponseType(typeof(Course), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    public async Task<IActionResult> UpdateCourse(int courseId, [FromBody] CourseSaveModel model)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        if (courseId < 1) return BadRequest();
        try
        {
            Course course = await courseService.UpdateCourseAsync(courseId, model.Code, model.Name);
            return Ok(course);
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
    /// Deletes a course and all its associated data.
    /// </summary>
    [HttpDelete("{courseId}")]
    [Authorize(Policy = ApiConstants.AdminOnlyPolicy)]
    [ProducesResponseType((int)HttpStatusCode.NoContent)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    public async Task<IActionResult> DeleteCourse(int courseId)
    {
        if (courseId < 1) return BadRequest();
        try
        {
            await courseService.DeleteCourseAsync(courseId);
            return NoContent();
        }
        catch (DataNotFoundException)
        {
            return NotFound();
        }
    }
}