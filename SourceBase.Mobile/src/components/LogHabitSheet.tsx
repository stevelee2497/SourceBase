import React, {
  forwardRef,
  useImperativeHandle,
  useMemo,
  useRef,
  useState,
} from "react";
import { View, Text, Pressable, TextInput, StyleSheet } from "react-native";
import BottomSheet, { BottomSheetView } from "@gorhom/bottom-sheet";
import { useHabits } from "../hooks/useHabits";
import { useCreateHabitLog } from "../hooks/useHabitLogs";
import { ACTION_META, ALL_ACTIONS } from "../utils/habitAction";
import { HabitLogAction } from "../models";
import { toApiDateTime } from "../utils/date";
import { colors, radius, spacing } from "../theme/colors";

export interface LogHabitSheetRef {
  open: () => void;
}

export const LogHabitSheet = forwardRef<LogHabitSheetRef>((_props, ref) => {
  const sheet = useRef<BottomSheet>(null);
  const habits = useHabits();
  const create = useCreateHabitLog();
  const [habitId, setHabitId] = useState<string | undefined>();
  const [habitName, setHabitName] = useState("");
  const [action, setAction] = useState<HabitLogAction>("HabitStarted");

  const snapPoints = useMemo(() => ["72%"], []);

  useImperativeHandle(ref, () => ({
    open: () => {
      setHabitId(undefined);
      setHabitName("");
      setAction("HabitStarted");
      sheet.current?.expand();
    },
  }));

  const submit = async () => {
    if (!habitId && !habitName) return;
    await create.mutateAsync([
      {
        habitId,
        habitName: habitId ? undefined : habitName || undefined,
        action,
        occurredAt: toApiDateTime(new Date()),
      },
    ]);
    sheet.current?.close();
  };

  const canSubmit = !!habitId || !!habitName;

  return (
    <BottomSheet ref={sheet} index={-1} snapPoints={snapPoints} enablePanDownToClose>
      <BottomSheetView style={styles.container}>
        <Text style={styles.title}>Log a habit</Text>

        <Text style={styles.label}>Habit</Text>
        <View style={styles.wrapRow}>
          {habits.data?.map((h) => (
            <Pressable
              key={h.id}
              onPress={() => {
                setHabitId(h.id);
                setHabitName("");
              }}
              style={[
                styles.chip,
                { borderColor: habitId === h.id ? colors.primary : colors.border },
              ]}
            >
              <Text>
                {h.icon ?? "🔁"} {h.name}
              </Text>
            </Pressable>
          ))}
        </View>
        <TextInput
          placeholder="…or type a habit name"
          value={habitName}
          onChangeText={(t) => {
            setHabitName(t);
            setHabitId(undefined);
          }}
          style={styles.input}
        />

        <Text style={styles.label}>Action</Text>
        <View style={styles.wrapRow}>
          {ALL_ACTIONS.map((a) => {
            const active = action === a;
            return (
              <Pressable
                key={a}
                onPress={() => setAction(a)}
                style={[
                  styles.actionChip,
                  { backgroundColor: active ? colors.primaryTint : colors.surface },
                ]}
              >
                <Text
                  style={{
                    color: active ? colors.primary : "#334155",
                    fontWeight: "600",
                  }}
                >
                  {ACTION_META[a].label}
                </Text>
              </Pressable>
            );
          })}
        </View>

        <Pressable
          onPress={submit}
          disabled={create.isPending || !canSubmit}
          style={[
            styles.primaryBtn,
            { backgroundColor: canSubmit ? colors.primary : "#cbd5e1" },
          ]}
        >
          <Text style={styles.primaryBtnText}>Save</Text>
        </Pressable>
      </BottomSheetView>
    </BottomSheet>
  );
});

LogHabitSheet.displayName = "LogHabitSheet";

const styles = StyleSheet.create({
  container: { padding: spacing.lg, gap: spacing.md },
  title: { fontSize: 18, fontWeight: "700" },
  label: { color: colors.textMuted },
  wrapRow: { flexDirection: "row", flexWrap: "wrap", gap: spacing.sm },
  chip: { padding: spacing.md, borderRadius: radius.md, borderWidth: 1 },
  actionChip: { padding: spacing.md, borderRadius: radius.md },
  input: {
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: radius.md,
    padding: spacing.md,
  },
  primaryBtn: {
    padding: spacing.lg,
    borderRadius: radius.lg,
    alignItems: "center",
    marginTop: spacing.sm,
  },
  primaryBtnText: { color: colors.white, fontWeight: "700" },
});
