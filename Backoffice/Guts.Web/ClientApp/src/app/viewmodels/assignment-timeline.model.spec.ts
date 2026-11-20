import {
  classifyTimelineDot,
  findNearestRunIndex,
  getTimelineChangeSummary,
  getTimelinePointXValue,
  hasDistinctTimestamps,
  IAssignmentTimelinePointModel,
  parseUtcTimestampMs,
} from "./assignment-timeline.model";

describe("assignment-timeline.model helpers", () => {
  const makePoint = (
    overrides: Partial<IAssignmentTimelinePointModel> = {},
  ): IAssignmentTimelinePointModel => ({
    testRunId: 1,
    runNumber: 1,
    createDateTimeUtc: "2026-11-20T10:00:00Z",
    passedTestsCount: 1,
    failedTestsCount: 2,
    totalTestsCount: 3,
    testResultsChanged: false,
    solutionFilesChanged: false,
    testResultsDelta: 0,
    ...overrides,
  });

  it("classifyTimelineDot should categorize all 5 dot tiers", () => {
    expect(
      classifyTimelineDot(
        makePoint({ testResultsChanged: true, testResultsDelta: 1, runNumber: 2 }),
      ),
    ).toBe("test-improved");

    expect(
      classifyTimelineDot(
        makePoint({
          testResultsChanged: true,
          testResultsDelta: 0,
          runNumber: 1,
          passedTestsCount: 3,
          totalTestsCount: 3,
        }),
      ),
    ).toBe("test-improved");

    expect(
      classifyTimelineDot(
        makePoint({ testResultsChanged: true, testResultsDelta: -1, runNumber: 3 }),
      ),
    ).toBe("test-regressed");

    expect(
      classifyTimelineDot(
        makePoint({ testResultsChanged: true, testResultsDelta: 0, runNumber: 1 }),
      ),
    ).toBe("test-neutral");

    expect(
      classifyTimelineDot(
        makePoint({ testResultsChanged: false, solutionFilesChanged: true }),
      ),
    ).toBe("code-only");

    expect(
      classifyTimelineDot(
        makePoint({ testResultsChanged: false, solutionFilesChanged: false }),
      ),
    ).toBe("unchanged");
  });

  it("getTimelineChangeSummary should return descriptive labels", () => {
    expect(
      getTimelineChangeSummary(
        makePoint({ testResultsChanged: true, solutionFilesChanged: true }),
      ),
    ).toBe("Test results & source code changed");

    expect(
      getTimelineChangeSummary(
        makePoint({ testResultsChanged: true, solutionFilesChanged: false }),
      ),
    ).toBe("Test results changed");

    expect(
      getTimelineChangeSummary(
        makePoint({ testResultsChanged: false, solutionFilesChanged: true }),
      ),
    ).toBe("Source code changed");

    expect(getTimelineChangeSummary(makePoint())).toBe("No changes");
  });

  it("parseUtcTimestampMs and hasDistinctTimestamps should handle UTC, naive, empty, and identical dates", () => {
    expect(parseUtcTimestampMs("")).toBe(0);
    expect(parseUtcTimestampMs("invalid-date")).toBe(0);
    expect(parseUtcTimestampMs("2026-11-20T10:00:00")).toBe(
      Date.parse("2026-11-20T10:00:00Z"),
    );

    expect(hasDistinctTimestamps([])).toBeFalse();
    expect(hasDistinctTimestamps([makePoint()])).toBeFalse();
    expect(
      hasDistinctTimestamps([
        makePoint({ createDateTimeUtc: "2026-11-20T10:00:00Z" }),
        makePoint({ createDateTimeUtc: "2026-11-20T10:00:00Z" }),
      ]),
    ).toBeFalse();
    expect(
      hasDistinctTimestamps([
        makePoint({ createDateTimeUtc: "2026-11-20T10:00:00Z" }),
        makePoint({ createDateTimeUtc: "2026-11-20T10:05:00Z" }),
      ]),
    ).toBeTrue();
  });

  it("getTimelinePointXValue and findNearestRunIndex should support both sequence and realtime modes", () => {
    const runs = [
      makePoint({ testRunId: 1, runNumber: 1, createDateTimeUtc: "2026-11-20T10:00:00Z" }),
      makePoint({ testRunId: 2, runNumber: 2, createDateTimeUtc: "2026-11-20T10:10:00Z" }),
      makePoint({ testRunId: 3, runNumber: 3, createDateTimeUtc: "2026-11-20T11:00:00Z" }),
    ];

    expect(getTimelinePointXValue(runs, 1, "sequence")).toBe(1);
    expect(getTimelinePointXValue(runs, 1, "realtime")).toBe(
      Date.parse("2026-11-20T10:10:00Z"),
    );

    expect(findNearestRunIndex([], 5, "sequence")).toBe(0);
    expect(findNearestRunIndex(runs, 1.4, "sequence")).toBe(1);
    expect(findNearestRunIndex(runs, 1.7, "sequence")).toBe(2);
    expect(
      findNearestRunIndex(
        runs,
        Date.parse("2026-11-20T10:12:00Z"),
        "realtime",
      ),
    ).toBe(1);

    // Fallback when all timestamps identical
    const sameTimeRuns = [
      makePoint({ createDateTimeUtc: "2026-11-20T10:00:00Z" }),
      makePoint({ createDateTimeUtc: "2026-11-20T10:00:00Z" }),
    ];
    expect(getTimelinePointXValue(sameTimeRuns, 1, "realtime")).toBe(1);
  });
});
