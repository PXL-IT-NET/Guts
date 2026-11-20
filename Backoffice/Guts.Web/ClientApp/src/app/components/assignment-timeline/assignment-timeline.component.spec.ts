import { ComponentFixture, TestBed } from "@angular/core/testing";
import { provideCharts, withDefaultRegisterables } from "ng2-charts";
import { IAssignmentTimelineModel } from "../../viewmodels/assignment-timeline.model";
import { AssignmentTimelineComponent } from "./assignment-timeline.component";

describe("AssignmentTimelineComponent", () => {
  let component: AssignmentTimelineComponent;
  let fixture: ComponentFixture<AssignmentTimelineComponent>;

  const sampleTimeline: IAssignmentTimelineModel = {
    assignmentId: 42,
    totalTestsCount: 4,
    runs: [
      {
        testRunId: 101,
        runNumber: 1,
        createDateTimeUtc: "2026-11-20T10:00:00Z",
        passedTestsCount: 1,
        failedTestsCount: 3,
        totalTestsCount: 4,
        testResultsChanged: true,
        solutionFilesChanged: true,
        testResultsDelta: 0,
      },
      {
        testRunId: 102,
        runNumber: 2,
        createDateTimeUtc: "2026-11-20T10:15:00Z",
        passedTestsCount: 1,
        failedTestsCount: 3,
        totalTestsCount: 4,
        testResultsChanged: false,
        solutionFilesChanged: true,
        testResultsDelta: 0,
      },
      {
        testRunId: 103,
        runNumber: 3,
        createDateTimeUtc: "2026-11-20T10:30:00Z",
        passedTestsCount: 3,
        failedTestsCount: 1,
        totalTestsCount: 4,
        testResultsChanged: true,
        solutionFilesChanged: true,
        testResultsDelta: 2,
      },
      {
        testRunId: 104,
        runNumber: 4,
        createDateTimeUtc: "2026-11-20T10:45:00Z",
        passedTestsCount: 2,
        failedTestsCount: 2,
        totalTestsCount: 4,
        testResultsChanged: true,
        solutionFilesChanged: false,
        testResultsDelta: -1,
      },
      {
        testRunId: 105,
        runNumber: 5,
        createDateTimeUtc: "2026-11-20T11:00:00Z",
        passedTestsCount: 2,
        failedTestsCount: 2,
        totalTestsCount: 4,
        testResultsChanged: false,
        solutionFilesChanged: false,
        testResultsDelta: 0,
      },
    ],
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AssignmentTimelineComponent],
      providers: [provideCharts(withDefaultRegisterables())],
    }).compileComponents();

    fixture = TestBed.createComponent(AssignmentTimelineComponent);
    component = fixture.componentInstance;
    component.timeline = sampleTimeline;
    component.selectedIndex = sampleTimeline.runs.length - 1;
    component.ngOnChanges();
    fixture.detectChanges();
  });

  afterEach(() => {
    fixture.destroy();
  });

  it("should create and build 3-tier dot styles in chart dataset", () => {
    expect(component).toBeTruthy();
    expect(component.chartData.datasets).toHaveSize(1);

    const dataset = component.chartData.datasets[0] as unknown as {
      clip: boolean;
      pointRadius: number[];
      pointBackgroundColor: string[];
      pointBorderColor: string[];
    };
    expect(dataset.clip).toBeFalse();
    expect(dataset.pointRadius).toEqual([7, 6, 7, 7, 3.5]);
    expect(dataset.pointBackgroundColor).toEqual([
      "#0d6efd", // neutral first run
      "#ffffff", // code-only hollow ring
      "#198754", // improved
      "#dc3545", // regressed
      "#adb5bd", // unchanged
    ]);
    expect(dataset.pointBorderColor[1]).toBe("#f59e0b");
  });

  it("should expose readout getters and empty-state safety", () => {
    component.selectedIndex = 2; // Run #3 (+2)
    expect(component.activePoint?.testRunId).toBe(103);
    expect(component.activeDeltaLabel).toBe("+2");
    expect(component.activeDotTier).toBe("test-improved");
    expect(component.activeChangeSummary).toBe(
      "Test results & source code changed",
    );
    expect(component.activeAriaValueText).toContain("Run 3 of 5");

    component.selectedIndex = 3; // Run #4 (-1)
    expect(component.activeDeltaLabel).toBe("-1");

    component.timeline = null;
    component.ngOnChanges();
    expect(component.runs).toEqual([]);
    expect(component.activeIndex).toBe(0);
    expect(component.activePoint).toBeNull();
    expect(component.activeTimestampFormatted).toBe("");
    expect(component.activeChangeSummary).toBe("");
    expect(component.activeDotTier).toBe("unchanged");
    expect(component.activeDeltaLabel).toBe("");
    expect(component.activeAriaValueText).toBe("");
  });

  it("should step with prev/next buttons and keyboard shortcuts", () => {
    const emitted: number[] = [];
    component.selectedIndexChange.subscribe((idx) => emitted.push(idx));

    component.selectedIndex = 4;
    component.selectNext(); // already at end -> no emit
    expect(emitted).toEqual([]);

    component.selectPrev();
    expect(emitted).toEqual([3]);

    component.selectedIndex = 0;
    component.selectPrev(); // already at start -> no emit
    expect(emitted).toEqual([3]);

    const mkEvent = (key: string) => {
      const ev = new KeyboardEvent("keydown", { key, cancelable: true });
      spyOn(ev, "preventDefault");
      return ev;
    };

    component.selectedIndex = 2;
    const leftEv = mkEvent("ArrowLeft");
    component.onKeyDown(leftEv);
    expect(leftEv.preventDefault).toHaveBeenCalled();

    const rightEv = mkEvent("ArrowRight");
    component.onKeyDown(rightEv);
    expect(rightEv.preventDefault).toHaveBeenCalled();

    const homeEv = mkEvent("Home");
    component.onKeyDown(homeEv);
    expect(homeEv.preventDefault).toHaveBeenCalled();

    const endEv = mkEvent("End");
    component.onKeyDown(endEv);
    expect(endEv.preventDefault).toHaveBeenCalled();

    const otherEv = mkEvent("Tab");
    component.onKeyDown(otherEv);
    expect(otherEv.preventDefault).not.toHaveBeenCalled();

    component.onRangeInput({ target: { value: "2" } } as unknown as Event);
    component.onRangeInput({ target: null } as unknown as Event);

    expect(emitted).toEqual([3, 1, 3, 0, 4, 1]);
  });

  it("should toggle between sequence and realtime scale modes and fall back when timestamps are identical", () => {
    expect(component.scaleMode()).toBe("sequence");
    expect(component.canUseRealtimeScale).toBeTrue();

    component.setScaleMode("realtime");
    expect(component.scaleMode()).toBe("realtime");
    // Calling again is a no-op
    component.setScaleMode("realtime");
    expect(component.scaleMode()).toBe("realtime");

    // Identical timestamps -> ngOnChanges resets scaleMode to sequence and prevents realtime
    component.timeline = {
      assignmentId: 42,
      totalTestsCount: 2,
      runs: [
        { ...sampleTimeline.runs[0], createDateTimeUtc: "2026-11-20T10:00:00Z" },
        { ...sampleTimeline.runs[1], createDateTimeUtc: "2026-11-20T10:00:00Z" },
      ],
    };
    component.ngOnChanges();
    expect(component.scaleMode()).toBe("sequence");
    expect(component.canUseRealtimeScale).toBeFalse();
    component.setScaleMode("realtime");
    expect(component.scaleMode()).toBe("sequence");
  });

  it("should invoke tooltip and axis tick callbacks cleanly", () => {
    const tooltipCallbacks = component.chartOptions?.plugins?.tooltip?.callbacks;
    expect(tooltipCallbacks).toBeDefined();

    const titleFn = tooltipCallbacks?.title as (
      items: Array<{ dataIndex: number }>,
    ) => string;
    const labelFn = tooltipCallbacks?.label as (item: {
      dataIndex: number;
    }) => string;
    const afterLabelFn = tooltipCallbacks?.afterLabel as (item: {
      dataIndex: number;
    }) => string;

    expect(titleFn([{ dataIndex: 2 }])).toContain("Run #3 of 5");
    expect(titleFn([])).toBe("");
    expect(labelFn({ dataIndex: 2 })).toBe("Passing: 3 / 4 (+2)");
    expect(labelFn({ dataIndex: 3 })).toBe("Passing: 2 / 4 (-1)");
    expect(labelFn({ dataIndex: 4 })).toBe("Passing: 2 / 4");
    expect(afterLabelFn({ dataIndex: 1 })).toBe("Source code changed");

    const xTickCb = (
      component.chartOptions?.scales?.["x"]?.ticks as {
        callback: (v: number | string) => string;
      }
    ).callback;
    expect(xTickCb(0)).toBe("#1");
    expect(xTickCb(2.5)).toBe("");
    expect(xTickCb("not-a-number")).toBe("");

    component.setScaleMode("realtime");
    const realtimeTickCb = (
      component.chartOptions?.scales?.["x"]?.ticks as {
        callback: (v: number | string) => string;
      }
    ).callback;
    expect(realtimeTickCb(Date.parse("2026-11-20T10:00:00Z"))).toContain(
      "20/11/2026",
    );
  });

  it("should drag beam with live preview and emit selectedIndexChange only on pointerup", () => {
    const emitted: number[] = [];
    component.selectedIndexChange.subscribe((idx) => emitted.push(idx));

    const drawSpy = jasmine.createSpy("draw");
    const fakeCanvas = {
      getBoundingClientRect: () => ({ left: 100 }),
    };
    const fakeChart = {
      canvas: fakeCanvas,
      chartArea: { left: 0, right: 400, top: 10, bottom: 150 },
      scales: {
        x: {
          getValueForPixel: (px: number) => px / 100, // 0..400 -> 0..4
          getPixelForValue: (v: number) => v * 100,
        },
        y: {
          getPixelForValue: (v: number) => 150 - v * 20,
        },
      },
      ctx: {
        save: jasmine.createSpy("save"),
        restore: jasmine.createSpy("restore"),
        fillRect: jasmine.createSpy("fillRect"),
        beginPath: jasmine.createSpy("beginPath"),
        moveTo: jasmine.createSpy("moveTo"),
        lineTo: jasmine.createSpy("lineTo"),
        stroke: jasmine.createSpy("stroke"),
        arc: jasmine.createSpy("arc"),
        fillStyle: "",
        strokeStyle: "",
        lineWidth: 0,
      },
      draw: drawSpy,
    };
    component.chartDirective = {
      chart: fakeChart,
    } as unknown as typeof component.chartDirective;

    // Right-click should be ignored
    component.onPointerDown({
      pointerType: "mouse",
      button: 2,
      clientX: 200,
      pointerId: 1,
    } as unknown as PointerEvent);
    expect(component.isDragging()).toBeFalse();

    // Pointer move while not dragging is ignored
    component.onPointerMove({ clientX: 200 } as unknown as PointerEvent);
    expect(component.previewIndex()).toBeNull();

    // Pointer down at x=300 (pixelX=200 -> index 2)
    component.onPointerDown({
      pointerType: "mouse",
      button: 0,
      clientX: 300,
      pointerId: 1,
    } as unknown as PointerEvent);
    expect(component.isDragging()).toBeTrue();
    expect(component.previewIndex()).toBe(2);
    expect(emitted).toEqual([]);

    // Drag to x=200 (pixelX=100 -> index 1)
    component.onPointerMove({ clientX: 200 } as unknown as PointerEvent);
    expect(component.previewIndex()).toBe(1);
    expect(emitted).toEqual([]);

    // Release at x=200 -> commits index 1
    component.onPointerUp({
      clientX: 200,
      pointerId: 1,
    } as unknown as PointerEvent);
    expect(component.isDragging()).toBeFalse();
    expect(component.previewIndex()).toBeNull();
    expect(emitted).toEqual([1]);

    // Invoke custom beam plugin afterDatasetsDraw directly to verify drawing calls
    const beamPlugin = component.chartPlugins[0];
    beamPlugin.afterDatasetsDraw?.(
      fakeChart as unknown as Parameters<
        NonNullable<typeof beamPlugin.afterDatasetsDraw>
      >[0],
      {} as never,
      {} as never,
      false,
    );
    expect(fakeChart.ctx.save).toHaveBeenCalled();
    expect(fakeChart.ctx.stroke).toHaveBeenCalled();
    expect(fakeChart.ctx.restore).toHaveBeenCalled();

    // Pointer cancel resets dragging without emitting
    component.onPointerDown({
      pointerType: "touch",
      button: 0,
      clientX: 100,
      pointerId: 2,
    } as unknown as PointerEvent);
    expect(component.isDragging()).toBeTrue();
    component.onPointerCancel();
    expect(component.isDragging()).toBeFalse();
    expect(component.previewIndex()).toBeNull();
    expect(emitted).toEqual([1]);
  });

  it("should handle edge-case pointer, keyboard, tooltip, and plugin guards safely", () => {
    const tooltipCallbacks = component.chartOptions?.plugins?.tooltip?.callbacks;
    const titleFn = tooltipCallbacks?.title as (
      items: Array<{ dataIndex: number }>,
    ) => string;
    const labelFn = tooltipCallbacks?.label as (item: {
      dataIndex: number;
    }) => string;
    const afterLabelFn = tooltipCallbacks?.afterLabel as (item: {
      dataIndex: number;
    }) => string;

    expect(titleFn([{ dataIndex: 99 }])).toBe("");
    expect(labelFn({ dataIndex: 99 })).toBe("");
    expect(afterLabelFn({ dataIndex: 99 })).toBe("");

    // Pointer capture throwing / releasing paths & non-dragging up/cancel
    component.onPointerUp({ clientX: 0, pointerId: 1 } as unknown as PointerEvent);
    component.onPointerCancel();

    const captureTarget = {
      setPointerCapture: jasmine.createSpy("setPointerCapture").and.throwError("unsupported"),
      releasePointerCapture: jasmine.createSpy("releasePointerCapture").and.throwError("unsupported"),
    };
    component.chartDirective = {
      chart: {
        canvas: { getBoundingClientRect: () => ({ left: 0 }) },
        chartArea: { left: 0, right: 100, top: 0, bottom: 100 },
        scales: { x: { getValueForPixel: () => undefined } },
        draw: jasmine.createSpy("draw"),
      },
    } as unknown as typeof component.chartDirective;
    component.onPointerDown({
      pointerType: "mouse",
      button: 0,
      clientX: 50,
      pointerId: 1,
      currentTarget: captureTarget,
    } as unknown as PointerEvent);
    component.onPointerUp({
      clientX: 50,
      pointerId: 1,
      currentTarget: captureTarget,
    } as unknown as PointerEvent);
    expect(captureTarget.setPointerCapture).toHaveBeenCalled();
    expect(captureTarget.releasePointerCapture).toHaveBeenCalled();

    // Plugin guards when scales missing or runs empty
    const beamPlugin = component.chartPlugins[0];
    beamPlugin.afterDatasetsDraw?.(
      { scales: {}, chartArea: undefined } as unknown as Parameters<
        NonNullable<typeof beamPlugin.afterDatasetsDraw>
      >[0],
      {} as never,
      {} as never,
      false,
    );

    component.timeline = null;
    component.ngOnChanges();
    component.onKeyDown(new KeyboardEvent("keydown", { key: "ArrowLeft" }));
    component.onPointerDown({
      pointerType: "mouse",
      button: 0,
      clientX: 50,
      pointerId: 1,
    } as unknown as PointerEvent);
    expect(component.resolveNearestIndexFromClientX(50)).toBeNull();
    beamPlugin.afterDatasetsDraw?.(
      { scales: {}, chartArea: undefined } as unknown as Parameters<
        NonNullable<typeof beamPlugin.afterDatasetsDraw>
      >[0],
      {} as never,
      {} as never,
      false,
    );
  });
});
