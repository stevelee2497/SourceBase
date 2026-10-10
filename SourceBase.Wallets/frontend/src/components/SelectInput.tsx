import { useState } from "react";
import { Icon } from "./Icon";

export type SelectOption = { value: string; label: string; icon?: string };

const BUTTON = "w-full flex items-center justify-between gap-2 rounded-md border bg-white dark:bg-gray-800 px-3 py-2 text-left text-sm shadow-sm transition-colors focus:outline-none";
const PANEL = "absolute z-201 top-full left-0 mt-1 min-w-full rounded-md border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 shadow-lg";

export const Chevron = ({ open }: { open: boolean }) => (
  <svg className={`h-4 w-4 shrink-0 text-gray-400 dark:text-gray-500 transition-transform duration-150 ${open ? "rotate-180" : ""}`} fill="none" stroke="currentColor" viewBox="0 0 20 20">
    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="1.5" d="M6 8l4 4 4-4" />
  </svg>
);

export const buttonClass = (open: boolean, disabled?: boolean) => `${BUTTON} ${disabled ? "cursor-not-allowed opacity-50" : "cursor-pointer"} ${open ? "border-indigo-500 ring-1 ring-indigo-500" : "border-gray-300 dark:border-gray-600 hover:border-gray-400 dark:hover:border-gray-500"}`;
export { PANEL as panelClass };

type Props = { value: string; onChange: (value: string) => void; options: SelectOption[]; disabled?: boolean; className?: string };

export const SelectInput = ({ value, onChange, options, disabled, className = "" }: Props) => {
  const [open, setOpen] = useState(false);
  const selected = options.find((o) => o.value === value);
  const pick = (v: string) => { setOpen(false); onChange(v); };

  return (
    <div className={`relative ${className}`}>
      <button type="button" className={buttonClass(open, disabled)} disabled={disabled} onClick={() => setOpen(!open)}>
        <span className={`flex items-center gap-2 truncate min-w-0 ${selected ? "text-gray-900 dark:text-gray-100" : "text-gray-400 dark:text-gray-500"}`}>
          {selected?.icon && <Icon value={selected.icon} size="w-4 h-4" />}
          <span className="truncate">{selected?.label ?? "Select…"}</span>
        </span>
        <Chevron open={open} />
      </button>
      {open && (
        <>
          <div className="fixed inset-0 z-200" onClick={() => setOpen(false)}></div>
          <div className={PANEL} style={{ maxHeight: "15rem", overflowY: "auto" }}>
            {options.map((opt) => {
              const isSelected = opt.value === value;
              return (
                <button key={opt.value} type="button" onClick={() => pick(opt.value)} className={`flex w-full items-center justify-between gap-3 px-3 py-2 text-left text-sm transition-colors ${isSelected ? "bg-indigo-50 dark:bg-indigo-900/30 text-indigo-700 dark:text-indigo-400 font-medium" : "text-gray-700 dark:text-gray-200 hover:bg-gray-50 dark:hover:bg-gray-700"}`}>
                  <span className="flex items-center gap-2 truncate min-w-0">
                    {opt.icon && <Icon value={opt.icon} size="w-4 h-4" />}
                    <span className="truncate">{opt.label}</span>
                  </span>
                  {isSelected && (
                    <svg className="h-3.5 w-3.5 shrink-0 text-indigo-500 dark:text-indigo-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2.5" d="M5 13l4 4L19 7" />
                    </svg>
                  )}
                </button>
              );
            })}
          </div>
        </>
      )}
    </div>
  );
};
