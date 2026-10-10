import { describe, expect, it } from "vitest";
import type { Wallet } from "../../lib/types";
import { PIE_COLORS, buildPieData } from "./pieData";

const wallet = (id: string, balance: number): Wallet => ({ id, name: `W${id}`, balance, initialBalance: 0, currency: "VND" });

describe("buildPieData", () => {
  it("returns nothing without wallets", () => expect(buildPieData([], [])).toEqual([]));

  it("splits proportionally across all wallets when none selected", () => {
    const slices = buildPieData([wallet("1", 300), wallet("2", 100)], []);
    expect(slices.map((s) => s.fraction)).toEqual([0.75, 0.25]);
    expect(slices.map((s) => s.color)).toEqual([PIE_COLORS[0], PIE_COLORS[1]]);
  });

  it("uses only selected wallets", () => {
    const slices = buildPieData([wallet("1", 300), wallet("2", 100)], ["2"]);
    expect(slices).toEqual([{ name: "W2", fraction: 1, color: PIE_COLORS[0] }]);
  });

  it("clamps negative balances and drops zero slices", () => {
    const slices = buildPieData([wallet("1", -50), wallet("2", 100), wallet("3", 0)], []);
    expect(slices).toEqual([{ name: "W2", fraction: 1, color: PIE_COLORS[1] }]);
  });

  it("falls back to equal slices when the total is not positive", () => {
    const slices = buildPieData([wallet("1", -10), wallet("2", 0)], []);
    expect(slices.map((s) => s.fraction)).toEqual([0.5, 0.5]);
  });
});
