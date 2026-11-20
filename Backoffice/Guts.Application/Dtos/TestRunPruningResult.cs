namespace Guts.Application.Dtos;

public sealed record TestRunPruningResult(
    int AssignmentsScanned,
    int TestRunsDeleted,
    int TestResultsDeleted);
