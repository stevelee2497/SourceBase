import { formatAmount } from "../lib/format";

type Props = { value: number; onChange: (value: number) => void; className?: string; placeholder?: string };

/** Digits-only amount field, displayed with thousands separators. Zero shows as empty. */
export const CurrencyInput = ({ value, onChange, className = "", placeholder = "0" }: Props) => (
  <input
    className={className}
    placeholder={placeholder}
    inputMode="numeric"
    value={value > 0 ? formatAmount(value) : ""}
    onChange={(e) => onChange(Number(e.target.value.replace(/\D/g, "")) || 0)}
  />
);
