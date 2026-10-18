using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Guts.Api.Models.ProjectModels;

public class CreateProjectAssessmentModel
{
    [Range(1, int.MaxValue)]
    public int ProjectId { get; set; }

    [Required]
    public string Description { get; set; }

    [JsonRequired]
    public DateTime OpenOn { get; set; }

    [JsonRequired]
    public DateTime Deadline { get; set; }
}