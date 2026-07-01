import React, { useMemo, useRef, useState } from "react";
import { View, Text, Pressable, StyleSheet } from "react-native";
import { FlashList } from "@shopify/flash-list";
import { Ionicons } from "@expo/vector-icons";
import groupBy from "lodash/groupBy";
import {
  useHabitLogs,
  useDeleteHabitLog,
} from "../../../src/hooks/useHabitLogs";
import { ACTION_META, ALL_ACTIONS } from "../../../src/utils/habitAction";
import { HabitLogAction } from "../../../src/models";
import { dayKey, dayHeading, timeLabel } from "../../../src/utils/date";
import {
  LogHabitSheet,
  LogHabitSheetRef,
} from "../../../src/components/LogHabitSheet";
import { Chip } from "../../../src/components/Chip";
import { Fab } from "../../../src/components/Fab";
import { EmptyState } from "../../../src/components/EmptyState";
import { colors, spacing } from "../../../src/theme/colors";

type Row =
  | { kind: "header"; day: string }
  | {
      kind: "row";
      log: {
        id: string;
        habitName?: string;
        action: HabitLogAction;
        occurredAt: string;
      };
    };

export default function Habits() {
  const [action, setAction] = useState<HabitLogAction | undefined>();
  const logs = useHabitLogs({ action });
  const del = useDeleteHabitLog();
  const sheetRef = useRef<LogHabitSheetRef>(null);

  const rows: Row[] = useMemo(() => {
    const flat = logs.data?.pages.flatMap((p) => p.items) ?? [];
    const byDay = groupBy(flat, (l) => dayKey(l.occurredAt));
    return Object.keys(byDay)
      .sort((a, b) => (a < b ? 1 : -1))
      .flatMap((day) => [
        { kind: "header" as const, day },
        ...byDay[day].map((l) => ({ kind: "row" as const, log: l })),
      ]);
  }, [logs.data]);

  return (
    <View style={{ flex: 1, backgroundColor: colors.white }}>
      {/* Filter chips */}
      <View style={styles.chips}>
        <Chip label="All" active={!action} onPress={() => setAction(undefined)} />
        {ALL_ACTIONS.map((a) => (
          <Chip
            key={a}
            label={ACTION_META[a].label}
            active={action === a}
            onPress={() => setAction(a)}
          />
        ))}
      </View>

      <FlashList
        data={rows}
        keyExtractor={(it, i) => (it.kind === "header" ? `h-${it.day}` : it.log.id) + i}
        getItemType={(it) => it.kind}
        onEndReached={() => logs.hasNextPage && logs.fetchNextPage()}
        onEndReachedThreshold={0.5}
        ListEmptyComponent={
          !logs.isLoading ? (
            <EmptyState
              title="No habit logs"
              subtitle="Logs from your desktop app appear here. Tap + to add one manually."
            />
          ) : null
        }
        renderItem={({ item }) => {
          if (item.kind === "header") {
            return <Text style={styles.dayHeader}>{dayHeading(item.day)}</Text>;
          }
          const meta = ACTION_META[item.log.action];
          return (
            <Pressable onLongPress={() => del.mutate(item.log.id)} style={styles.row}>
              <Ionicons name={meta.icon as any} size={22} color={meta.color} />
              <View style={{ flex: 1 }}>
                <Text style={{ fontWeight: "600" }}>{item.log.habitName ?? "Habit"}</Text>
                <Text style={styles.sub}>{meta.label}</Text>
              </View>
              <Text style={styles.time}>{timeLabel(item.log.occurredAt)}</Text>
            </Pressable>
          );
        }}
      />

      <Fab onPress={() => sheetRef.current?.open()} />
      <LogHabitSheet ref={sheetRef} />
    </View>
  );
}

const styles = StyleSheet.create({
  chips: {
    flexDirection: "row",
    flexWrap: "wrap",
    gap: spacing.sm,
    padding: spacing.md,
  },
  dayHeader: {
    paddingHorizontal: spacing.lg,
    paddingTop: spacing.lg,
    paddingBottom: spacing.xs,
    color: colors.textMuted,
    fontWeight: "700",
  },
  row: {
    flexDirection: "row",
    alignItems: "center",
    gap: spacing.md,
    padding: spacing.lg,
  },
  sub: { color: colors.textMuted, fontSize: 12 },
  time: { color: colors.textFaint, fontSize: 12 },
});
