import { IPeriodModel } from "../viewmodels/period.model";
import { PeriodProvider } from "./period.provider";

describe("PeriodProvider", () => {
  it("should get and set period and emit on period$", () => {
    const provider = new PeriodProvider();
    expect(provider.period).toBeNull();

    const period: IPeriodModel = {
      id: 1,
      description: "2025-2026",
      from: null,
      until: null,
      isActive: true,
    };

    let emitted: IPeriodModel = null;
    provider.period$.subscribe((p) => (emitted = p));

    provider.period = period;

    expect(provider.period).toBe(period);
    expect(emitted).toBe(period);
  });
});

