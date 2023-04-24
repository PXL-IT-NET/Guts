using System;
using System.ComponentModel.DataAnnotations;

namespace Guts.Api.Models.ProjectModels;

public class CreateProjectAssessmentModel
{
    [Range(1, int.MaxValue)]
    public int ProjectId { get; set; }

    [Required]
    public string Description { get; set; }
    public DateTime OpenOn { get; set; }
    public DateTime Deadline { get; set; }
}