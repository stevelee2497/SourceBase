const amountFormat = new Intl.NumberFormat("en-US", { maximumFractionDigits: 0 });
const MONTHS = ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"];

export const formatAmount = (amount: number) => amountFormat.format(amount);
export const signedAmount = (amount: number, type: string) => `${type === "Income" ? "+" : "-"}${formatAmount(amount)}`;
export const formatPercent = (fraction: number) => `${(fraction * 100).toFixed(1)}%`;

/** yyyy-MM-dd -> "05 Oct" (the value is returned as is when it isn't a date). */
export const formatDisplayDate = (value: string) => {
  const match = /^\d{4}-(\d{2})-(\d{2})$/.exec(value);
  return match ? `${match[2]} ${MONTHS[Number(match[1]) - 1]}` : value;
};

export const toIsoDate = (date: Date) => `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, "0")}-${String(date.getDate()).padStart(2, "0")}`;
export const todayIso = () => toIsoDate(new Date());
