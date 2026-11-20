using System.Collections.Generic;
using Guts.Domain.TestRunAggregate;

namespace Guts.Application.Dtos;

public sealed record AssignmentTimelineDto(
    int AssignmentId,
    int TotalTestsCount,
    IReadOnlyList<TestRunTimelinePoint> Runs);
