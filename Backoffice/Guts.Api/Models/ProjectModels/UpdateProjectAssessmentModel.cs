using System;
using System.ComponentModel.DataAnnotations;

namespace Guts.Api.Models.ProjectModels;

public class UpdateProjectAssessmentModel
{
    public int Id { get; set; }

    [Required]
    public string Description { get; set; }
    public DateTime OpenOn { get; set; }
    public DateTime Deadline { get; set; }
}