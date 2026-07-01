export const formatMoney = (amount: number, currency = "USD") => {
  try {
    return new Intl.NumberFormat(undefined, {
      style: "currency",
      currency,
      maximumFractionDigits: 2,
    }).format(amount);
  } catch {
    // Fallback if the runtime lacks ICU for the given currency code.
    return `${amount.toFixed(2)} ${currency}`;
  }
};
