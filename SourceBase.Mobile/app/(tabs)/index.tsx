import React, { useState, useCallback } from "react";
import { ScrollView, View, Text, RefreshControl, StyleSheet } from "react-native";
import { SafeAreaView } from "react-native-safe-area-context";
import { useQueryClient } from "@tanstack/react-query";
import {
  useStats,
  useRecentTransactions,
  useTodayOpenCount,
} from "../../src/hooks/useStats";
import { formatMoney } from "../../src/utils/money";
import { labelDate } from "../../src/utils/date";
import { colors, radius, spacing } from "../../src/theme/colors";

export default function Home() {
  const qc = useQueryClient();
  const stats = useStats();
  const recent = useRecentTransactions();
  const todayOpen = useTodayOpenCount();
  const [refreshing, setRefreshing] = useState(false);

  const onRefresh = useCallback(async () => {
    setRefreshing(true);
    await qc.invalidateQueries();
    setRefreshing(false);
  }, [qc]);

  const s = stats.data;

  return (
    <SafeAreaView edges={["top"]} style={{ flex: 1 }}>
    <ScrollView
      contentContainerStyle={styles.content}
      refreshControl={<RefreshControl refreshing={refreshing} onRefresh={onRefresh} />}
    >
      {/* Balance hero */}
      <View style={styles.hero}>
        <Text style={styles.heroLabel}>Total balance</Text>
        <Text style={styles.heroValue}>{s ? formatMoney(s.totalBalance) : "—"}</Text>
      </View>

      {/* Month income / expense */}
      <View style={styles.row}>
        <View style={[styles.card, { backgroundColor: colors.incomeBg }]}>
          <Text style={{ color: colors.incomeText }}>Income (month)</Text>
          <Text style={[styles.cardValue, { color: colors.income }]}>
            {s ? formatMoney(s.monthlyIncome) : "—"}
          </Text>
        </View>
        <View style={[styles.card, { backgroundColor: colors.expenseBg }]}>
          <Text style={{ color: colors.expenseText }}>Expense (month)</Text>
          <Text style={[styles.cardValue, { color: colors.expense }]}>
            {s ? formatMoney(s.monthlyExpense) : "—"}
          </Text>
        </View>
      </View>

      {/* Today's open todos */}
      <View style={styles.todoCard}>
        <Text style={{ fontWeight: "600" }}>Open tasks today</Text>
        <Text style={styles.todoCount}>{todayOpen.data ?? "—"}</Text>
      </View>

      {/* Recent activity */}
      <View style={{ gap: spacing.sm }}>
        <Text style={styles.sectionTitle}>Recent activity</Text>
        {recent.data?.items.length ? (
          recent.data.items.map((t) => (
            <View key={t.id} style={styles.txnRow}>
              <View>
                <Text style={{ fontWeight: "600" }}>{t.categoryName ?? t.walletName}</Text>
                <Text style={styles.txnDate}>{labelDate(t.date)}</Text>
              </View>
              <Text
                style={{
                  color: t.type === "Income" ? colors.income : colors.expense,
                  fontWeight: "700",
                }}
              >
                {t.type === "Income" ? "+" : "-"}
                {formatMoney(Math.abs(t.amount))}
              </Text>
            </View>
          ))
        ) : (
          <Text style={styles.txnDate}>No transactions yet.</Text>
        )}
      </View>
    </ScrollView>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  content: { padding: spacing.lg, gap: spacing.lg },
  hero: { backgroundColor: colors.primary, borderRadius: radius.xl, padding: spacing.xl },
  heroLabel: { color: colors.primaryText },
  heroValue: { color: colors.white, fontSize: 32, fontWeight: "800" },
  row: { flexDirection: "row", gap: spacing.md },
  card: { flex: 1, borderRadius: radius.lg, padding: spacing.lg },
  cardValue: { fontSize: 20, fontWeight: "700" },
  todoCard: { backgroundColor: colors.surfaceAlt, borderRadius: radius.lg, padding: spacing.lg },
  todoCount: { fontSize: 24, fontWeight: "800" },
  sectionTitle: { fontWeight: "700", fontSize: 16 },
  txnRow: { flexDirection: "row", justifyContent: "space-between", paddingVertical: spacing.sm },
  txnDate: { color: colors.textMuted, fontSize: 12 },
});
