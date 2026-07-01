import React, { useState, useMemo, useRef } from "react";
import { View, Text, Pressable, StyleSheet } from "react-native";
import { FlashList } from "@shopify/flash-list";
import { useLocalSearchParams, Stack } from "expo-router";
import groupBy from "lodash/groupBy";
import { useWallet } from "../../../src/hooks/useWallets";
import { useTransactions } from "../../../src/hooks/useTransactions";
import { formatMoney } from "../../../src/utils/money";
import { dayHeading } from "../../../src/utils/date";
import {
  TransactionSheet,
  TransactionSheetRef,
} from "../../../src/components/TransactionSheet";
import { SummaryView } from "../../../src/components/SummaryView";
import { Fab } from "../../../src/components/Fab";
import { EmptyState } from "../../../src/components/EmptyState";
import { Transaction } from "../../../src/models";
import { colors, radius, spacing } from "../../../src/theme/colors";

type Tab = "txns" | "summary";
type Row =
  | { kind: "header"; day: string }
  | { kind: "row"; txn: Transaction };

export default function WalletDetail() {
  const { id } = useLocalSearchParams<{ id: string }>();
  const wallet = useWallet(id!);
  const [tab, setTab] = useState<Tab>("txns");
  const sheetRef = useRef<TransactionSheetRef>(null);

  const txns = useTransactions({ walletId: id });

  // Flatten pages, then group by date into header + rows for a Money Lover timeline.
  const rows: Row[] = useMemo(() => {
    const flat = txns.data?.pages.flatMap((p) => p.items) ?? [];
    const byDay = groupBy(flat, (t) => t.date);
    return Object.keys(byDay)
      .sort((a, b) => (a < b ? 1 : -1))
      .flatMap((day) => [
        { kind: "header" as const, day },
        ...byDay[day].map((t) => ({ kind: "row" as const, txn: t })),
      ]);
  }, [txns.data]);

  const currency = wallet.data?.currency;

  return (
    <View style={{ flex: 1, backgroundColor: colors.white }}>
      <Stack.Screen options={{ title: wallet.data?.name ?? "Wallet" }} />

      {/* Balance header */}
      <View style={styles.header}>
        <Text style={styles.headerLabel}>{wallet.data?.name}</Text>
        <Text style={styles.headerValue}>
          {wallet.data ? formatMoney(wallet.data.balance, currency) : "—"}
        </Text>
      </View>

      {/* Segmented control */}
      <View style={styles.segment}>
        {(["txns", "summary"] as Tab[]).map((t) => {
          const active = tab === t;
          return (
            <Pressable
              key={t}
              onPress={() => setTab(t)}
              style={[
                styles.segmentBtn,
                { backgroundColor: active ? colors.primaryTint : "transparent" },
              ]}
            >
              <Text style={{ color: active ? colors.primary : colors.textMuted, fontWeight: "600" }}>
                {t === "txns" ? "Transactions" : "Summary"}
              </Text>
            </Pressable>
          );
        })}
      </View>

      {tab === "txns" ? (
        <FlashList
          data={rows}
          estimatedItemSize={64}
          keyExtractor={(it, i) => (it.kind === "header" ? `h-${it.day}` : it.txn.id) + i}
          getItemType={(it) => it.kind}
          onEndReached={() => txns.hasNextPage && txns.fetchNextPage()}
          onEndReachedThreshold={0.5}
          ListEmptyComponent={
            !txns.isLoading ? (
              <EmptyState
                title="No transactions"
                subtitle="Tap + to add your first one."
              />
            ) : null
          }
          renderItem={({ item }) => {
            if (item.kind === "header") {
              return <Text style={styles.dayHeader}>{dayHeading(item.day)}</Text>;
            }
            const t = item.txn;
            return (
              <Pressable onPress={() => sheetRef.current?.openEdit(t)} style={styles.txnRow}>
                <View style={{ flex: 1 }}>
                  <Text style={{ fontWeight: "600" }}>{t.categoryName ?? "Uncategorized"}</Text>
                  <Text style={styles.note}>{t.note ?? ""}</Text>
                </View>
                <Text
                  style={{
                    color: t.type === "Income" ? colors.income : colors.expense,
                    fontWeight: "700",
                  }}
                >
                  {t.type === "Income" ? "+" : "-"}
                  {formatMoney(Math.abs(t.amount), currency)}
                </Text>
              </Pressable>
            );
          }}
        />
      ) : (
        <SummaryView walletId={id!} currency={currency} />
      )}

      <Fab onPress={() => sheetRef.current?.openNew(id!)} />
      <TransactionSheet ref={sheetRef} />
    </View>
  );
}

const styles = StyleSheet.create({
  header: { padding: spacing.xl, backgroundColor: colors.primary },
  headerLabel: { color: colors.primaryText },
  headerValue: { color: colors.white, fontSize: 28, fontWeight: "800" },
  segment: { flexDirection: "row", padding: spacing.sm, gap: spacing.sm },
  segmentBtn: {
    flex: 1,
    padding: spacing.md,
    borderRadius: radius.md,
    alignItems: "center",
  },
  dayHeader: {
    paddingHorizontal: spacing.lg,
    paddingTop: spacing.lg,
    paddingBottom: spacing.xs,
    color: colors.textMuted,
    fontWeight: "700",
  },
  txnRow: {
    flexDirection: "row",
    justifyContent: "space-between",
    alignItems: "center",
    padding: spacing.lg,
  },
  note: { color: colors.textMuted, fontSize: 12 },
});
