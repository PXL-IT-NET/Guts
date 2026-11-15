import { calculateTotalPages, createPagedSearchParams } from "./paged-result";

describe("paged-result helpers", () => {
  it("calculateTotalPages should return 1 when pageSize is null or <= 0, otherwise ceil", () => {
    expect(calculateTotalPages(100, null)).toBe(1);
    expect(calculateTotalPages(100, 0)).toBe(1);
    expect(calculateTotalPages(0, 25)).toBe(1);
    expect(calculateTotalPages(51, 25)).toBe(3);
  });

  it("createPagedSearchParams should include search when non-blank and pageSize when non-null", () => {
    const withAll = createPagedSearchParams("  alice  ", 2, 50);
    expect(withAll.get("page")).toBe("2");
    expect(withAll.get("search")).toBe("alice");
    expect(withAll.get("pageSize")).toBe("50");

    const withoutOptional = createPagedSearchParams("   ", 1, null);
    expect(withoutOptional.get("page")).toBe("1");
    expect(withoutOptional.has("search")).toBeFalse();
    expect(withoutOptional.has("pageSize")).toBeFalse();
  });
});
