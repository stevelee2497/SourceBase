import { describe, expect, it } from "vitest";
import { DATE_RANGE, mergeDateRanges, resolveDateRange } from "./dateRanges";
import { formatAmount, formatDisplayDate, formatPercent, signedAmount } from "./format";

const TODAY = new Date(2026, 9, 10);
const NO_CUSTOM = { from: null, to: null };

describe("format", () => {
  it("formats amounts without decimals and with separators", () => expect(formatAmount(1234567.8)).toBe("1,234,568"));
  it("signs amounts by type", () => {
    expect(signedAmount(1500, "Income")).toBe("+1,500");
    expect(signedAmount(1500, "Expense")).toBe("-1,500");
  });
  it("formats dates as dd MMM", () => expect(formatDisplayDate("2026-10-05")).toBe("05 Oct"));
  it("formats percentages with one decimal", () => expect(formatPercent(0.12345)).toBe("12.3%"));
});

describe("dateRanges", () => {
  it("resolves this month, last month and this year", () => {
    expect(resolveDateRange(DATE_RANGE.thisMonth, TODAY)).toEqual({ from: "2026-10-01", to: "2026-10-31" });
    expect(resolveDateRange(DATE_RANGE.lastMonth, TODAY)).toEqual({ from: "2026-09-01", to: "2026-09-30" });
    expect(resolveDateRange(DATE_RANGE.lastThreeMonths, TODAY)).toEqual({ from: "2026-08-01", to: "2026-10-31" });
    expect(resolveDateRange(DATE_RANGE.thisYear, TODAY)).toEqual({ from: "2026-01-01", to: "2026-12-31" });
  });

  it("is unbounded for All", () => expect(resolveDateRange(DATE_RANGE.all, TODAY)).toEqual(NO_CUSTOM));

  it("merges several ranges into the covering span", () => {
    expect(mergeDateRanges([DATE_RANGE.thisMonth, DATE_RANGE.lastMonth], NO_CUSTOM, TODAY)).toEqual({ from: "2026-09-01", to: "2026-10-31" });
  });

  it("stays unbounded when any selected range is unbounded", () => {
    expect(mergeDateRanges([DATE_RANGE.thisMonth, DATE_RANGE.all], NO_CUSTOM, TODAY)).toEqual(NO_CUSTOM);
  });

  it("uses the custom range for Custom", () => {
    expect(mergeDateRanges([DATE_RANGE.custom], { from: "2026-02-01", to: "2026-02-28" }, TODAY)).toEqual({ from: "2026-02-01", to: "2026-02-28" });
  });

  it("is unbounded with nothing selected", () => expect(mergeDateRanges([], NO_CUSTOM, TODAY)).toEqual(NO_CUSTOM));
});
