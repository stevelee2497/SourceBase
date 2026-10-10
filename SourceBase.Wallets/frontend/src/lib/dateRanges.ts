import { toIsoDate } from "./format";

export const DATE_RANGE = { thisMonth: "this-month", lastMonth: "last-month", lastThreeMonths: "last-3-months", thisYear: "this-year", all: "all", custom: "custom" } as const;

export const DATE_RANGE_OPTIONS = [
  { value: DATE_RANGE.thisMonth, label: "This month" },
  { value: DATE_RANGE.lastMonth, label: "Last Month" },
  { value: DATE_RANGE.lastThreeMonths, label: "Last 3 months" },
  { value: DATE_RANGE.thisYear, label: "This Year" },
  { value: DATE_RANGE.all, label: "All" },
  { value: DATE_RANGE.custom, label: "Custom" },
];

export type Range = { from: string | null; to: string | null };
export type Custom = { from: string | null; to: string | null };

const monthStart = (today: Date, offset: number) => new Date(today.getFullYear(), today.getMonth() + offset, 1);
const dayBefore = (date: Date) => new Date(date.getFullYear(), date.getMonth(), date.getDate() - 1);
const range = (from: Date, to: Date): Range => ({ from: toIsoDate(from), to: toIsoDate(to) });

export const resolveDateRange = (value: string, today = new Date()): Range => {
  switch (value) {
    case DATE_RANGE.thisMonth: return range(monthStart(today, 0), dayBefore(monthStart(today, 1)));
    case DATE_RANGE.lastMonth: return range(monthStart(today, -1), dayBefore(monthStart(today, 0)));
    case DATE_RANGE.lastThreeMonths: return range(monthStart(today, -2), dayBefore(monthStart(today, 1)));
    case DATE_RANGE.thisYear: return range(new Date(today.getFullYear(), 0, 1), new Date(today.getFullYear(), 11, 31));
    default: return { from: null, to: null };
  }
};

/** Several selected ranges collapse into the single span that covers them all; an unbounded end stays unbounded. */
export const mergeDateRanges = (selected: string[], custom: Custom, today = new Date()): Range => {
  const ranges = selected.map((value) => (value === DATE_RANGE.custom ? custom : resolveDateRange(value, today)));
  if (ranges.length === 0) return { from: null, to: null };
  const bound = (values: (string | null)[], pick: (a: string, b: string) => string) => (values.some((v) => v === null) ? null : values.reduce((a, b) => pick(a!, b!)));
  return { from: bound(ranges.map((r) => r.from), (a, b) => (a < b ? a : b)), to: bound(ranges.map((r) => r.to), (a, b) => (a > b ? a : b)) };
};
