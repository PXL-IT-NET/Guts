export type TimelineScaleMode = "sequence" | "realtime";

export type TimelineDotTier =
  | "test-improved"
  | "test-regressed"
  | "test-neutral"
  | "code-only"
  | "unchanged";

export interface IAssignmentTimelinePointModel {
  testRunId: number;
  runNumber: number;
  createDateTimeUtc: string;
  passedTestsCount: number;
  failedTestsCount: number;
  totalTestsCount: number;
  testResultsChanged: boolean;
  solutionFilesChanged: boolean;
  testResultsDelta: number;
}

export interface IAssignmentTimelineModel {
  assignmentId: number;
  totalTestsCount: number;
  runs: IAssignmentTimelinePointModel[];
}

export function classifyTimelineDot(
  point: IAssignmentTimelinePointModel,
): TimelineDotTier {
  if (point.testResultsChanged) {
    if (
      point.testResultsDelta > 0 ||
      (point.runNumber === 1 &&
        point.totalTestsCount > 0 &&
        point.passedTestsCount === point.totalTestsCount)
    ) {
      return "test-improved";
    }
    if (point.testResultsDelta < 0) {
      return "test-regressed";
    }
    return "test-neutral";
  }

  if (point.solutionFilesChanged) {
    return "code-only";
  }

  return "unchanged";
}

export function getTimelineChangeSummary(
  point: IAssignmentTimelinePointModel,
): string {
  if (point.testResultsChanged && point.solutionFilesChanged) {
    return "Test results & source code changed";
  }
  if (point.testResultsChanged) {
    return "Test results changed";
  }
  if (point.solutionFilesChanged) {
    return "Source code changed";
  }
  return "No changes";
}

export function parseUtcTimestampMs(value: string): number {
  if (!value) {
    return 0;
  }
  const normalized =
    !value.endsWith("Z") && !value.includes("+") ? `${value}Z` : value;
  const parsed = Date.parse(normalized);
  return Number.isNaN(parsed) ? 0 : parsed;
}

export function hasDistinctTimestamps(
  runs: IAssignmentTimelinePointModel[],
): boolean {
  if (!runs || runs.length <= 1) {
    return false;
  }
  const first = parseUtcTimestampMs(runs[0].createDateTimeUtc);
  const last = parseUtcTimestampMs(runs.at(-1)!.createDateTimeUtc);
  return last > first;
}

export function getTimelinePointXValue(
  runs: IAssignmentTimelinePointModel[],
  index: number,
  scaleMode: TimelineScaleMode,
): number {
  if (scaleMode === "realtime" && hasDistinctTimestamps(runs)) {
    return parseUtcTimestampMs(runs[index].createDateTimeUtc);
  }
  return index;
}

export function findNearestRunIndex(
  runs: IAssignmentTimelinePointModel[],
  xValue: number,
  scaleMode: TimelineScaleMode,
): number {
  if (!runs || runs.length === 0) {
    return 0;
  }

  let bestIndex = 0;
  let bestDistance = Math.abs(
    getTimelinePointXValue(runs, 0, scaleMode) - xValue,
  );

  for (let i = 1; i < runs.length; i++) {
    const distance = Math.abs(
      getTimelinePointXValue(runs, i, scaleMode) - xValue,
    );
    if (distance < bestDistance) {
      bestDistance = distance;
      bestIndex = i;
    }
  }

  return bestIndex;
}
