import type { Wallet } from "../../lib/types";
import type { SelectOption } from "../../components/SelectInput";

export const TRANSACTION_TYPE = { income: "Income", expense: "Expense" } as const;
export const PAGE_SIZE = 20;
export const DEFAULT_CURRENCY = "VND";
export const SELECT_WALLET_OPTION: SelectOption = { value: "", label: "Select wallet" };

export const walletOption = (w: Wallet): SelectOption => ({ value: w.id, label: `${w.name} (${w.currency})`, icon: w.icon });
export const optionalText = (value: string) => value.trim() || undefined;
