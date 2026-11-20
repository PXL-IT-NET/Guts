import { CommonModule } from "@angular/common";
import {
  ChangeDetectionStrategy,
  Component,
  EventEmitter,
  Input,
  OnChanges,
  Output,
  ViewChild,
  signal,
} from "@angular/core";
import { ChartConfiguration, Plugin } from "chart.js";
import { BaseChartDirective } from "ng2-charts";
import { formatDateTime } from "../../util/date.util";
import {
  IAssignmentTimelineModel,
  IAssignmentTimelinePointModel,
  TimelineDotTier,
  TimelineScaleMode,
  classifyTimelineDot,
  findNearestRunIndex,
  getTimelineChangeSummary,
  getTimelinePointXValue,
  hasDistinctTimestamps,
} from "../../viewmodels/assignment-timeline.model";

interface TimelineDotStyle {
  radius: number;
  hoverRadius: number;
  backgroundColor: string;
  borderColor: string;
  borderWidth: number;
}

const DOT_STYLES: Record<TimelineDotTier, TimelineDotStyle> = {
  "test-improved": {
    radius: 7,
    hoverRadius: 9,
    backgroundColor: "#198754",
    borderColor: "#198754",
    borderWidth: 2,
  },
  "test-regressed": {
    radius: 7,
    hoverRadius: 9,
    backgroundColor: "#dc3545",
    borderColor: "#dc3545",
    borderWidth: 2,
  },
  "test-neutral": {
    radius: 7,
    hoverRadius: 9,
    backgroundColor: "#0d6efd",
    borderColor: "#0d6efd",
    borderWidth: 2,
  },
  "code-only": {
    radius: 6,
    hoverRadius: 8,
    backgroundColor: "#ffffff",
    borderColor: "#f59e0b",
    borderWidth: 2.5,
  },
  unchanged: {
    radius: 3.5,
    hoverRadius: 6,
    backgroundColor: "#adb5bd",
    borderColor: "#6c757d",
    borderWidth: 1,
  },
};

@Component({
  standalone: true,
  selector: "app-assignment-timeline",
  imports: [CommonModule, BaseChartDirective],
  templateUrl: "./assignment-timeline.component.html",
  styleUrls: ["./assignment-timeline.component.scss"],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AssignmentTimelineComponent implements OnChanges {
  @Input() public timeline: IAssignmentTimelineModel | null = null;
  @Input() public selectedIndex = 0;
  @Input() public isLoadingSnapshot = false;
  @Output() public readonly selectedIndexChange = new EventEmitter<number>();

  @ViewChild(BaseChartDirective) public chartDirective?: BaseChartDirective;

  public readonly scaleMode = signal<TimelineScaleMode>("sequence");
  public readonly previewIndex = signal<number | null>(null);
  public readonly isDragging = signal<boolean>(false);

  public chartData: ChartConfiguration<"line">["data"] = { datasets: [] };
  public chartOptions: ChartConfiguration<"line">["options"] = {};
  public readonly chartPlugins: Plugin<"line">[] = [this.createBeamPlugin()];

  public get runs(): IAssignmentTimelinePointModel[] {
    return this.timeline?.runs ?? [];
  }

  public get activeIndex(): number {
    const runs = this.runs;
    if (runs.length === 0) {
      return 0;
    }
    const raw = this.previewIndex() ?? this.selectedIndex;
    return Math.max(0, Math.min(runs.length - 1, raw));
  }

  public get activePoint(): IAssignmentTimelinePointModel | null {
    const runs = this.runs;
    if (runs.length === 0) {
      return null;
    }
    return runs[this.activeIndex] ?? null;
  }

  public get canGoPrev(): boolean {
    return this.activeIndex > 0;
  }

  public get canGoNext(): boolean {
    return this.activeIndex < this.runs.length - 1;
  }

  public get canUseRealtimeScale(): boolean {
    return hasDistinctTimestamps(this.runs);
  }

  public get activeTimestampFormatted(): string {
    return this.activePoint
      ? formatDateTime(this.activePoint.createDateTimeUtc)
      : "";
  }

  public get activeChangeSummary(): string {
    return this.activePoint ? getTimelineChangeSummary(this.activePoint) : "";
  }

  public get activeDotTier(): TimelineDotTier {
    return this.activePoint
      ? classifyTimelineDot(this.activePoint)
      : "unchanged";
  }

  public get activeDeltaLabel(): string {
    if (!this.activePoint || this.activePoint.testResultsDelta === 0) {
      return "";
    }
    return this.activePoint.testResultsDelta > 0
      ? `+${this.activePoint.testResultsDelta}`
      : `${this.activePoint.testResultsDelta}`;
  }

  public get activeAriaValueText(): string {
    const point = this.activePoint;
    if (!point) {
      return "";
    }
    return `Run ${point.runNumber} of ${this.runs.length}, ${this.activeTimestampFormatted}, ${point.passedTestsCount} of ${point.totalTestsCount} tests passing, ${this.activeChangeSummary}`;
  }

  public ngOnChanges(): void {
    if (this.scaleMode() === "realtime" && !this.canUseRealtimeScale) {
      this.scaleMode.set("sequence");
    }
    this.rebuildChart();
    this.redrawBeam();
  }

  public setScaleMode(mode: TimelineScaleMode): void {
    if (mode === "realtime" && !this.canUseRealtimeScale) {
      return;
    }
    if (this.scaleMode() === mode) {
      return;
    }
    this.scaleMode.set(mode);
    this.rebuildChart();
    this.redrawBeam();
  }

  public selectPrev(): void {
    if (this.canGoPrev) {
      this.commitSelection(this.activeIndex - 1);
    }
  }

  public selectNext(): void {
    if (this.canGoNext) {
      this.commitSelection(this.activeIndex + 1);
    }
  }

  public onRangeInput(event: Event): void {
    const input = event.target as HTMLInputElement | null;
    if (!input) {
      return;
    }
    const runNumber = Number.parseInt(input.value, 10);
    if (!Number.isNaN(runNumber)) {
      this.commitSelection(runNumber - 1);
    }
  }

  public onKeyDown(event: KeyboardEvent): void {
    if (this.runs.length === 0) {
      return;
    }
    switch (event.key) {
      case "ArrowLeft":
        event.preventDefault();
        this.selectPrev();
        break;
      case "ArrowRight":
        event.preventDefault();
        this.selectNext();
        break;
      case "Home":
        event.preventDefault();
        this.commitSelection(0);
        break;
      case "End":
        event.preventDefault();
        this.commitSelection(this.runs.length - 1);
        break;
      default:
        break;
    }
  }

  public onPointerDown(event: PointerEvent): void {
    if (event.pointerType === "mouse" && event.button !== 0) {
      return;
    }
    if (this.runs.length === 0) {
      return;
    }
    const target = event.currentTarget as HTMLElement | null;
    if (target?.setPointerCapture) {
      try {
        target.setPointerCapture(event.pointerId);
      } catch {
        // Synthetic pointer events in tests may not have an active pointerId
      }
    }
    this.isDragging.set(true);
    const nearest = this.resolveNearestIndexFromClientX(event.clientX);
    if (nearest !== null) {
      this.previewIndex.set(nearest);
      this.redrawBeam();
    }
  }

  public onPointerMove(event: PointerEvent): void {
    if (!this.isDragging()) {
      return;
    }
    const nearest = this.resolveNearestIndexFromClientX(event.clientX);
    if (nearest !== null && nearest !== this.previewIndex()) {
      this.previewIndex.set(nearest);
      this.redrawBeam();
    }
  }

  public onPointerUp(event: PointerEvent): void {
    if (!this.isDragging()) {
      return;
    }
    const target = event.currentTarget as HTMLElement | null;
    if (target?.releasePointerCapture) {
      try {
        target.releasePointerCapture(event.pointerId);
      } catch {
        // Ignore if pointer capture was not active
      }
    }
    const nearest =
      this.resolveNearestIndexFromClientX(event.clientX) ?? this.previewIndex();
    this.isDragging.set(false);
    this.previewIndex.set(null);
    if (nearest !== null) {
      this.commitSelection(nearest);
    }
    this.redrawBeam();
  }

  public onPointerCancel(): void {
    if (!this.isDragging()) {
      return;
    }
    this.isDragging.set(false);
    this.previewIndex.set(null);
    this.redrawBeam();
  }

  public resolveNearestIndexFromClientX(clientX: number): number | null {
    const chart = this.chartDirective?.chart;
    const xScale = chart?.scales?.["x"];
    const chartArea = chart?.chartArea;
    if (!chart?.canvas || !xScale || !chartArea || this.runs.length === 0) {
      return null;
    }
    const rect = chart.canvas.getBoundingClientRect();
    const pixelX = clientX - rect.left;
    const clampedPixelX = Math.max(
      chartArea.left,
      Math.min(chartArea.right, pixelX),
    );
    const xValue = xScale.getValueForPixel(clampedPixelX);
    if (xValue === undefined || Number.isNaN(xValue)) {
      return null;
    }
    return findNearestRunIndex(this.runs, xValue, this.scaleMode());
  }

  private commitSelection(index: number): void {
    const runs = this.runs;
    if (runs.length === 0) {
      return;
    }
    const clamped = Math.max(0, Math.min(runs.length - 1, index));
    if (clamped !== this.selectedIndex) {
      this.selectedIndexChange.emit(clamped);
    }
  }

  private redrawBeam(): void {
    this.chartDirective?.chart?.draw();
  }

  private rebuildChart(): void {
    const runs = this.runs;
    const mode = this.scaleMode();
    const xValues = runs.map((_, idx) =>
      getTimelinePointXValue(runs, idx, mode),
    );
    const points = runs.map((point, idx) => ({
      x: xValues[idx],
      y: point.passedTestsCount,
    }));

    const styles = runs.map((point) => DOT_STYLES[classifyTimelineDot(point)]);

    this.chartData = {
      datasets: [
        {
          label: "Passing tests",
          data: points,
          borderColor: "#0d6efd",
          backgroundColor: "rgba(13, 110, 253, 0.08)",
          borderWidth: 2,
          clip: false,
          fill: false,
          tension: 0,
          pointRadius: styles.map((s) => s.radius),
          pointHoverRadius: styles.map((s) => s.hoverRadius),
          pointBackgroundColor: styles.map((s) => s.backgroundColor),
          pointBorderColor: styles.map((s) => s.borderColor),
          pointBorderWidth: styles.map((s) => s.borderWidth),
        },
      ],
    };

    const maxPassed = runs.reduce(
      (max, run) => Math.max(max, run.passedTestsCount),
      0,
    );
    const yMax = Math.max(this.timeline?.totalTestsCount ?? 0, maxPassed, 1);

    let xMin: number | undefined;
    let xMax: number | undefined;
    if (xValues.length > 0) {
      const firstX = xValues[0];
      const lastX = xValues.at(-1)!;
      const span = lastX - firstX;
      const padding = mode === "realtime" && span > 0 ? span * 0.04 : 0.35;
      xMin = firstX - padding;
      xMax = lastX + padding;
    }

    this.chartOptions = {
      responsive: true,
      maintainAspectRatio: false,
      animation: false,
      interaction: {
        mode: "nearest",
        axis: "x",
        intersect: false,
      },
      layout: {
        padding: {
          top: 26,
          right: 14,
          bottom: 6,
          left: 4,
        },
      },
      plugins: {
        legend: {
          display: false,
        },
        tooltip: {
          displayColors: false,
          callbacks: {
            title: (items) => {
              const item = items[0];
              if (!item) {
                return "";
              }
              const point = runs[item.dataIndex];
              if (!point) {
                return "";
              }
              return `Run #${point.runNumber} of ${runs.length} — ${formatDateTime(point.createDateTimeUtc)}`;
            },
            label: (item) => {
              const point = runs[item.dataIndex];
              if (!point) {
                return "";
              }
              let delta = "";
              if (point.testResultsDelta > 0) {
                delta = ` (+${point.testResultsDelta})`;
              } else if (point.testResultsDelta < 0) {
                delta = ` (${point.testResultsDelta})`;
              }
              return `Passing: ${point.passedTestsCount} / ${point.totalTestsCount}${delta}`;
            },
            afterLabel: (item) => {
              const point = runs[item.dataIndex];
              return point ? getTimelineChangeSummary(point) : "";
            },
          },
        },
      },
      scales: {
        x: {
          type: "linear",
          min: xMin,
          max: xMax,
          grid: {
            color: "rgba(0, 0, 0, 0.05)",
          },
          ticks: {
            maxTicksLimit: 6,
            callback: (value) => {
              const numeric = Number(value);
              if (Number.isNaN(numeric)) {
                return "";
              }
              if (mode === "realtime" && hasDistinctTimestamps(runs)) {
                return formatDateTime(numeric);
              }
              if (
                Number.isInteger(numeric) &&
                numeric >= 0 &&
                numeric < runs.length
              ) {
                return `#${runs[numeric].runNumber}`;
              }
              return "";
            },
          },
        },
        y: {
          beginAtZero: true,
          max: yMax,
          grid: {
            color: "rgba(0, 0, 0, 0.06)",
          },
          ticks: {
            precision: 0,
            stepSize: yMax <= 10 ? 1 : undefined,
          },
          title: {
            display: true,
            text: "Passing tests",
          },
        },
      },
    };
  }

  private createBeamPlugin(): Plugin<"line"> {
    return {
      id: "gutsTimelineBeam",
      afterDatasetsDraw: (chart) => {
        const runs = this.runs;
        if (runs.length === 0) {
          return;
        }
        const xScale = chart.scales["x"];
        const yScale = chart.scales["y"];
        const chartArea = chart.chartArea;
        if (!xScale || !yScale || !chartArea) {
          return;
        }
        const idx = this.activeIndex;
        const point = runs[idx];
        const xValue = getTimelinePointXValue(runs, idx, this.scaleMode());
        const beamX = xScale.getPixelForValue(xValue);
        const dotY = yScale.getPixelForValue(point.passedTestsCount);
        if (!Number.isFinite(beamX) || !Number.isFinite(dotY)) {
          return;
        }

        const ctx = chart.ctx;
        ctx.save();

        // Dim future region to the right of the beam
        if (beamX < chartArea.right) {
          ctx.fillStyle = "rgba(108, 117, 125, 0.10)";
          ctx.fillRect(
            beamX,
            chartArea.top,
            chartArea.right - beamX,
            chartArea.bottom - chartArea.top,
          );
        }

        // Vertical time-travel beam (extends slightly above chartArea to meet grab handle)
        const handleWidth = 20;
        const handleHeight = 10;
        const handleBottom = chartArea.top - 12;
        const beamColor = this.isDragging() ? "#0a58ca" : "#0d6efd";
        ctx.strokeStyle = beamColor;
        ctx.lineWidth = 2.5;
        ctx.beginPath();
        ctx.moveTo(beamX, handleBottom);
        ctx.lineTo(beamX, chartArea.bottom);
        ctx.stroke();

        // Top grab handle pill positioned cleanly above top-row dots
        ctx.fillStyle = beamColor;
        ctx.fillRect(
          beamX - handleWidth / 2,
          handleBottom - handleHeight,
          handleWidth,
          handleHeight,
        );

        // Active dot highlight ring
        ctx.strokeStyle = beamColor;
        ctx.lineWidth = 2;
        ctx.beginPath();
        ctx.arc(beamX, dotY, 10, 0, Math.PI * 2);
        ctx.stroke();

        ctx.restore();
      },
    };
  }
}
