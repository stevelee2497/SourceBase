import { useState } from "react";
import { Icon } from "./Icon";
import { Chevron, buttonClass, panelClass, type SelectOption } from "./SelectInput";

type Props = { values: string[]; onChange: (values: string[]) => void; options: SelectOption[]; placeholder?: string; disabled?: boolean; className?: string };

export const MultiSelectInput = ({ values, onChange, options, placeholder = "Select…", disabled, className = "" }: Props) => {
  const [open, setOpen] = useState(false);
  const selected = options.filter((o) => values.includes(o.value));
  const summary = selected.length === 0 ? placeholder : selected.length === 1 ? selected[0].label : `${selected.length} selected`;
  const toggle = (value: string) => onChange(values.includes(value) ? values.filter((v) => v !== value) : [...values, value]);

  return (
    <div className={`relative ${className}`}>
      <button type="button" className={buttonClass(open, disabled)} disabled={disabled} onClick={() => setOpen(!open)}>
        <span className="flex items-center gap-2 truncate min-w-0 text-gray-900 dark:text-gray-100">
          {selected.length === 1 && selected[0].icon && <Icon value={selected[0].icon} size="w-4 h-4" />}
          <span className="truncate">{summary}</span>
        </span>
        <Chevron open={open} />
      </button>
      {open && (
        <>
          <div className="fixed inset-0 z-200" onClick={() => setOpen(false)}></div>
          <div className={panelClass} style={{ maxHeight: "15rem", overflowY: "auto" }}>
            {selected.length > 0 && <button type="button" onClick={() => onChange([])} className="flex w-full items-center gap-2 border-b border-gray-100 dark:border-gray-700 px-3 py-1.5 text-left text-xs text-gray-500 dark:text-gray-400 hover:bg-gray-50 dark:hover:bg-gray-700">Clear all</button>}
            {options.map((opt) => {
              const isSelected = values.includes(opt.value);
              return (
                <button key={opt.value} type="button" onClick={() => toggle(opt.value)} className={`flex w-full items-center gap-3 px-3 py-2 text-left text-sm transition-colors ${isSelected ? "bg-indigo-50 dark:bg-indigo-900/30 text-indigo-700 dark:text-indigo-400 font-medium" : "text-gray-700 dark:text-gray-200 hover:bg-gray-50 dark:hover:bg-gray-700"}`}>
                  <span className={`flex h-4 w-4 shrink-0 items-center justify-center rounded border ${isSelected ? "border-indigo-500 bg-indigo-500 text-white" : "border-gray-300 dark:border-gray-600"}`}>
                    {isSelected && (
                      <svg className="h-3 w-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="3" d="M5 13l4 4L19 7" />
                      </svg>
                    )}
                  </span>
                  <span className="flex items-center gap-2 truncate min-w-0">
                    {opt.icon && <Icon value={opt.icon} size="w-4 h-4" />}
                    <span className="truncate">{opt.label}</span>
                  </span>
                </button>
              );
            })}
          </div>
        </>
      )}
    </div>
  );
};
