using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Guts.Api.Models.ProjectModels;

public class UpdateProjectAssessmentModel
{
    [JsonRequired]
    public int Id { get; set; }

    [Required]
    public string Description { get; set; }

    [JsonRequired]
    public DateTime OpenOn { get; set; }

    [JsonRequired]
    public DateTime Deadline { get; set; }
}