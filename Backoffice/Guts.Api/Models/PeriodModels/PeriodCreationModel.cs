using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Guts.Api.Models.PeriodModels;

public class PeriodCreationModel
{
    [Required]
    public string Description { get; set; }

    [Required]
    [JsonConverter(typeof(NullableDateTimeJsonConverter))]
    public DateTime? From { get; set; }

    [Required]
    [JsonConverter(typeof(NullableDateTimeJsonConverter))]
    public DateTime? Until { get; set; }
}