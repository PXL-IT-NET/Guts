using System;
using System.Text.Json.Serialization;

namespace Guts.Api.Models.PeriodModels;

public class PeriodCreationModel
{
    public string Description { get; set; }

    [JsonRequired]
    public DateTime From { get; set; }

    [JsonRequired]
    public DateTime Until { get; set; }
}