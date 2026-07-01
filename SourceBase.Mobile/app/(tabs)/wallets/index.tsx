import React from "react";
import { View, Text, Pressable, StyleSheet } from "react-native";
import { SafeAreaView } from "react-native-safe-area-context";
import { FlashList } from "@shopify/flash-list";
import { useRouter } from "expo-router";
import { useWallets } from "../../../src/hooks/useWallets";
import { formatMoney } from "../../../src/utils/money";
import { EmptyState } from "../../../src/components/EmptyState";
import { colors, radius, spacing } from "../../../src/theme/colors";

export default function WalletList() {
  const router = useRouter();
  const { data, isLoading, refetch } = useWallets();

  return (
    <SafeAreaView edges={["top"]} style={{ flex: 1, backgroundColor: colors.white }}>
      {/* Money Lover-style total header */}
      <View style={styles.header}>
        <Text style={styles.headerLabel}>Total balance</Text>
        <Text style={styles.headerValue}>
          {data ? formatMoney(data.totalBalance) : "—"}
        </Text>
      </View>

      <FlashList
        data={data?.wallets ?? []}
        refreshing={isLoading}
        onRefresh={refetch}
        keyExtractor={(w) => w.id}
        ListEmptyComponent={
          !isLoading ? (
            <EmptyState
              title="No wallets yet"
              subtitle="Create a wallet to start tracking your money."
            />
          ) : null
        }
        renderItem={({ item }) => (
          <Pressable
            onPress={() => router.push(`/wallets/${item.id}`)}
            style={styles.row}
          >
            <View style={styles.avatar}>
              <Text style={{ fontSize: 20 }}>{item.icon ?? "💰"}</Text>
            </View>
            <View style={{ flex: 1 }}>
              <Text style={styles.name}>{item.name}</Text>
              <Text style={styles.currency}>{item.currency}</Text>
            </View>
            <Text style={styles.balance}>
              {formatMoney(item.balance, item.currency)}
            </Text>
          </Pressable>
        )}
      />
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  header: { padding: spacing.xl, backgroundColor: colors.primary },
  headerLabel: { color: colors.primaryText },
  headerValue: { color: colors.white, fontSize: 30, fontWeight: "800" },
  row: {
    flexDirection: "row",
    alignItems: "center",
    padding: spacing.lg,
    gap: spacing.md,
  },
  avatar: {
    width: 44,
    height: 44,
    borderRadius: 22,
    backgroundColor: colors.primaryTint,
    alignItems: "center",
    justifyContent: "center",
  },
  name: { fontWeight: "600", fontSize: 16 },
  currency: { color: colors.textMuted, fontSize: 12 },
  balance: { fontWeight: "700" },
});
