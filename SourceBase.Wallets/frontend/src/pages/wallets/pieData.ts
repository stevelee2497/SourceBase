import type { Wallet } from "../../lib/types";

export const PIE_COLORS = ["#6366f1", "#8b5cf6", "#ec4899", "#f43f5e", "#f97316", "#eab308", "#22c55e", "#06b6d4", "#3b82f6", "#a855f7"];

export type PieSlice = { name: string; fraction: number; color: string };

/** Slices for the selected wallets (or all): negative balances count as 0, zero slices are dropped, equal slices when nothing is positive. */
export const buildPieData = (wallets: Wallet[], selectedIds: string[]): PieSlice[] => {
  const list = selectedIds.length > 0 ? wallets.filter((w) => selectedIds.includes(w.id)) : wallets;
  if (list.length === 0) return [];
  const positiveTotal = list.reduce((sum, w) => sum + Math.max(0, w.balance), 0);
  const color = (i: number) => PIE_COLORS[i % PIE_COLORS.length];
  if (positiveTotal <= 0) return list.map((w, i) => ({ name: w.name, fraction: 1 / list.length, color: color(i) }));
  return list.map((w, i) => ({ name: w.name, fraction: Math.max(0, w.balance) / positiveTotal, color: color(i) })).filter((s) => s.fraction > 0);
};
