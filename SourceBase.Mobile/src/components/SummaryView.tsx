import React from "react";
import { View, Text, StyleSheet } from "react-native";
import { PieChart } from "react-native-gifted-charts";
import { useSummary } from "../hooks/useTransactions";
import { formatMoney } from "../utils/money";
import { colors, radius, spacing } from "../theme/colors";

export function SummaryView({
  walletId,
  currency,
  dateFrom,
  dateTo,
}: {
  walletId: string;
  currency?: string;
  dateFrom?: string;
  dateTo?: string;
}) {
  const { data } = useSummary({ walletId, dateFrom, dateTo });

  const expenseSlices = (data?.byCategory ?? [])
    .filter((b) => b.type === "Expense")
    .map((b, i) => ({
      value: b.total,
      text: b.categoryName ?? "Other",
      color: colors.chart[i % colors.chart.length],
    }));

  return (
    <View style={styles.container}>
      <View style={styles.row}>
        <Stat label="Income" value={data?.totalIncome} color={colors.income} currency={currency} />
        <Stat label="Expense" value={data?.totalExpense} color={colors.expense} currency={currency} />
        <Stat label="Net" value={data?.netBalance} color={colors.text} currency={currency} />
      </View>

      {expenseSlices.length > 0 && (
        <View style={styles.chartWrap}>
          <PieChart data={expenseSlices} donut radius={110} innerRadius={70} />
          <View style={styles.legend}>
            {expenseSlices.map((s) => (
              <View key={s.text} style={styles.legendRow}>
                <View style={[styles.dot, { backgroundColor: s.color }]} />
                <Text style={styles.legendText}>
                  {s.text} · {formatMoney(s.value, currency)}
                </Text>
              </View>
            ))}
          </View>
        </View>
      )}
    </View>
  );
}

function Stat({
  label,
  value,
  color,
  currency,
}: {
  label: string;
  value?: number;
  color: string;
  currency?: string;
}) {
  return (
    <View style={styles.stat}>
      <Text style={styles.statLabel}>{label}</Text>
      <Text style={[styles.statValue, { color }]}>
        {value != null ? formatMoney(value, currency) : "—"}
      </Text>
    </View>
  );
}

const styles = StyleSheet.create({
  container: { padding: spacing.lg, gap: spacing.lg },
  row: { flexDirection: "row", gap: spacing.md },
  stat: {
    flex: 1,
    backgroundColor: colors.surfaceAlt,
    borderRadius: radius.lg,
    padding: spacing.md,
  },
  statLabel: { color: colors.textMuted, fontSize: 12 },
  statValue: { fontWeight: "700" },
  chartWrap: { alignItems: "center", gap: spacing.md },
  legend: { alignSelf: "stretch", gap: spacing.xs },
  legendRow: { flexDirection: "row", alignItems: "center", gap: spacing.sm },
  dot: { width: 10, height: 10, borderRadius: 5 },
  legendText: { color: colors.textMuted, fontSize: 12 },
});
