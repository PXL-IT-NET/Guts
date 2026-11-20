using System;
using System.Collections.Generic;

namespace Guts.Api.ScheduledJobs;

public class ScheduledJobsOptions
{
    public const string SectionName = "ScheduledJobs";
    public const string DefaultBrusselsTimeZoneId = "Europe/Brussels";

    public string DefaultTimeZoneId { get; set; } = DefaultBrusselsTimeZoneId;

    public Dictionary<string, ScheduledJobConfig> Jobs { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public class ScheduledJobConfig
{
    public bool Enabled { get; set; } = true;
    public string CronExpression { get; set; }
    public string TimeZoneId { get; set; }
}
