import React, { useEffect, useMemo, useState } from "react";
import {
  View,
  Text,
  Pressable,
  TextInput,
  KeyboardAvoidingView,
  Platform,
  StyleSheet,
} from "react-native";
import { SafeAreaView } from "react-native-safe-area-context";
import { FlashList } from "@shopify/flash-list";
import { Ionicons } from "@expo/vector-icons";
import { useAuth } from "../../../src/auth/AuthContext";
import {
  useTodoLists,
  useTodos,
  useCreateTodo,
  useToggleTodo,
  useDeleteTodo,
} from "../../../src/hooks/useTodos";
import { Todo } from "../../../src/models";
import { labelDate } from "../../../src/utils/date";
import { Chip } from "../../../src/components/Chip";
import { EmptyState } from "../../../src/components/EmptyState";
import { colors, spacing } from "../../../src/theme/colors";

type Row = { kind: "header"; label: string } | { kind: "row"; todo: Todo };

export default function Todos() {
  const { user } = useAuth();
  const lists = useTodoLists();
  const [listId, setListId] = useState<string | undefined>(user?.defaultTodoListId);

  // Choose a default list once data arrives.
  useEffect(() => {
    if (!listId && lists.data?.items.length) {
      setListId(
        lists.data.items.find((l) => l.isDefault)?.id ?? lists.data.items[0].id
      );
    }
  }, [lists.data, listId]);

  const filters = { todoListId: listId };
  const todos = useTodos(filters);
  const createTodo = useCreateTodo(filters);
  const toggle = useToggleTodo(filters);
  const del = useDeleteTodo();
  const [draft, setDraft] = useState("");

  const { open, completed } = useMemo(() => {
    const items = (todos.data?.items ?? []).filter((t) => t.status !== "Archived");
    return {
      open: items.filter((t) => t.status === "Open"),
      completed: items.filter((t) => t.status === "Completed"),
    };
  }, [todos.data]);

  const rows: Row[] = useMemo(
    () => [
      ...open.map((t) => ({ kind: "row" as const, todo: t })),
      ...(completed.length
        ? [{ kind: "header" as const, label: `Completed (${completed.length})` }]
        : []),
      ...completed.map((t) => ({ kind: "row" as const, todo: t })),
    ],
    [open, completed]
  );

  const add = () => {
    if (draft.trim()) {
      createTodo.mutate({ title: draft.trim() });
      setDraft("");
    }
  };

  return (
    <SafeAreaView edges={["top"]} style={{ flex: 1, backgroundColor: colors.white }}>
    <KeyboardAvoidingView
      style={{ flex: 1 }}
      behavior={Platform.OS === "ios" ? "padding" : undefined}
    >
      {/* List switcher */}
      <View style={styles.chips}>
        {lists.data?.items.map((l) => (
          <Chip
            key={l.id}
            label={`${l.name} · ${l.itemCount}`}
            active={listId === l.id}
            onPress={() => setListId(l.id)}
          />
        ))}
      </View>

      <FlashList
        data={rows}
        keyExtractor={(it, i) => (it.kind === "header" ? `h${i}` : it.todo.id)}
        getItemType={(it) => it.kind}
        ListEmptyComponent={
          listId && !todos.isLoading ? (
            <EmptyState title="All clear" subtitle="Add a task using the box below." />
          ) : null
        }
        renderItem={({ item }) => {
          if (item.kind === "header") {
            return <Text style={styles.header}>{item.label}</Text>;
          }
          const t = item.todo;
          const done = t.status === "Completed";
          return (
            <Pressable onLongPress={() => del.mutate(t.id)} style={styles.row}>
              <Pressable onPress={() => toggle.mutate(t)} hitSlop={10}>
                <Ionicons
                  name={done ? "checkmark-circle" : "ellipse-outline"}
                  size={24}
                  color={done ? colors.primary : colors.textFaint}
                />
              </Pressable>
              <View style={{ flex: 1 }}>
                <Text
                  style={{
                    textDecorationLine: done ? "line-through" : "none",
                    color: done ? colors.textFaint : colors.text,
                  }}
                >
                  {t.title}
                </Text>
                {t.date && <Text style={styles.date}>{labelDate(t.date)}</Text>}
              </View>
            </Pressable>
          );
        }}
      />

      {/* Persistent composer */}
      <View style={styles.composer}>
        <Ionicons name="add" size={22} color={colors.primary} />
        <TextInput
          placeholder="Add a task"
          value={draft}
          onChangeText={setDraft}
          onSubmitEditing={add}
          returnKeyType="done"
          style={styles.input}
        />
      </View>
    </KeyboardAvoidingView>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  chips: {
    flexDirection: "row",
    flexWrap: "wrap",
    gap: spacing.sm,
    padding: spacing.md,
  },
  header: {
    paddingHorizontal: spacing.lg,
    paddingTop: spacing.lg,
    color: colors.textMuted,
    fontWeight: "700",
  },
  row: {
    flexDirection: "row",
    alignItems: "center",
    gap: spacing.md,
    padding: spacing.lg,
  },
  date: { color: colors.textMuted, fontSize: 12 },
  composer: {
    flexDirection: "row",
    alignItems: "center",
    gap: spacing.sm,
    padding: spacing.md,
    borderTopWidth: 1,
    borderColor: "#eef2f7",
  },
  input: { flex: 1, padding: spacing.sm },
});
