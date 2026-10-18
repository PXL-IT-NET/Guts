import {
  formatDateTime,
  formatDateTimeLocal,
  toIsoDateString,
} from "./date.util";

describe("date.util", () => {
  it("formatDateTime should format date into DD/MM/YYYY HH:mm", () => {
    const d = new Date(2026, 4, 15, 14, 30);
    expect(formatDateTime(d)).toBe("15/05/2026 14:30");
    expect(formatDateTime(null)).toBe("");
    expect(formatDateTime(undefined)).toBe("");
  });

  it("formatDateTimeLocal should format date into YYYY-MM-DDTHH:mm", () => {
    const d = new Date(2026, 4, 15, 14, 30);
    expect(formatDateTimeLocal(d)).toBe("2026-05-15T14:30");
    expect(formatDateTimeLocal(null)).toBe("");
  });

  it("toIsoDateString should return ISO string for Date and string as is", () => {
    const d = new Date("2026-05-15T14:30:00.000Z");
    expect(toIsoDateString(d)).toBe("2026-05-15T14:30:00.000Z");
    expect(toIsoDateString("2026-05-15")).toBe("2026-05-15");
    expect(toIsoDateString(null)).toBe("");
  });
});
