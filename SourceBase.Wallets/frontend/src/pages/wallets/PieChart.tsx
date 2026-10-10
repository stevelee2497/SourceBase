import { formatAmount, formatPercent } from "../../lib/format";
import type { Wallet } from "../../lib/types";
import { buildPieData, type PieSlice } from "./pieData";

const point = (turns: number) => `${Math.cos(turns * 2 * Math.PI).toFixed(4)} ${Math.sin(turns * 2 * Math.PI).toFixed(4)}`;

const slicePaths = (slices: PieSlice[]) => {
  let cumulative = 0;
  return slices.map((slice) => {
    const start = cumulative;
    cumulative += slice.fraction;
    const largeArc = slice.fraction > 0.5 ? 1 : 0;
    return <path key={slice.name + start} d={`M 0 0 L ${point(start)} A 1 1 0 ${largeArc} 1 ${point(cumulative)} Z`} fill={slice.color} stroke="white" strokeWidth="0.02" />;
  });
};

type Props = { wallets: Wallet[]; selectedIds: string[] };

export const PieChart = ({ wallets, selectedIds }: Props) => {
  const slices = buildPieData(wallets, selectedIds);
  const selectedWallets = selectedIds.length > 0 ? wallets.filter((w) => selectedIds.includes(w.id)) : wallets;
  const selectedBalance = selectedWallets.reduce((sum, w) => sum + w.balance, 0);

  return (
    <div className="card flex flex-col sm:flex-row items-center gap-4 py-3 px-5">
      <div className="shrink-0">
        <svg width="140" height="140" viewBox="-1.02 -1.02 2.04 2.04" style={{ transform: "rotate(-90deg)" }}>
          {slices.length === 1 ? <circle cx="0" cy="0" r="1" fill={slices[0].color} /> : slicePaths(slices)}
        </svg>
      </div>
      <div className="flex flex-col gap-3 flex-1 justify-center items-center">
        <div className="flex flex-wrap gap-x-4 gap-y-1 justify-center">
          {slices.map((slice, i) => (
            <div key={i} className="flex items-center gap-1.5 text-xs">
              <div className="w-2.5 h-2.5 rounded-full shrink-0" style={{ backgroundColor: slice.color }}></div>
              <span className="text-gray-700 dark:text-gray-200 font-medium">{slice.name}</span>
              <span className="text-gray-400 dark:text-gray-500">{formatPercent(slice.fraction)}</span>
            </div>
          ))}
        </div>
        <div className="border-t border-gray-100 dark:border-gray-700 w-full text-center">
          <div className="text-sm text-gray-400 dark:text-gray-500 mt-1">{selectedIds.length > 0 ? "Selected balance" : "Total balance"}</div>
          <div className="text-lg font-bold text-gray-900 dark:text-gray-100">{formatAmount(selectedBalance)}</div>
        </div>
      </div>
    </div>
  );
};
