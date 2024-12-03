using System;

namespace Guts.Api.Models.PeriodModels;

public class PeriodCreationModel
{
    public string Description { get; set; }
    public DateTime From { get; set; }
    public DateTime Until { get; set; }
}