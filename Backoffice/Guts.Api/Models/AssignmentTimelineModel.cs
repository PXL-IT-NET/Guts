using System;
using System.Collections.Generic;

namespace Guts.Api.Models;

public class AssignmentTimelineModel
{
    public int AssignmentId { get; set; }
    public int TotalTestsCount { get; set; }
    public IReadOnlyList<AssignmentTimelinePointModel> Runs { get; set; } = [];
}

public class AssignmentTimelinePointModel
{
    public int TestRunId { get; set; }
    public int RunNumber { get; set; }
    public DateTime CreateDateTimeUtc { get; set; }
    public int PassedTestsCount { get; set; }
    public int FailedTestsCount { get; set; }
    public int TotalTestsCount { get; set; }
    public bool TestResultsChanged { get; set; }
    public bool SolutionFilesChanged { get; set; }
    public int TestResultsDelta { get; set; }
}
