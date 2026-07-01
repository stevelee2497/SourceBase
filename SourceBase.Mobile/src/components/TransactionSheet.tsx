import React, {
  forwardRef,
  useImperativeHandle,
  useMemo,
  useRef,
  useState,
} from "react";
import { View, Text, Pressable, TextInput, StyleSheet } from "react-native";
import BottomSheet, { BottomSheetView } from "@gorhom/bottom-sheet";
import {
  useCategories,
  useUpsertTransaction,
  useDeleteTransaction,
} from "../hooks/useTransactions";
import { toApiDate, fromApiDate } from "../utils/date";
import { Transaction, TransactionType } from "../models";
import { colors, radius, spacing } from "../theme/colors";

export interface TransactionSheetRef {
  openNew: (walletId: string) => void;
  openEdit: (t: Transaction) => void;
}

const KEYS = ["1", "2", "3", "4", "5", "6", "7", "8", "9", ".", "0", "del"];

export const TransactionSheet = forwardRef<TransactionSheetRef>((_props, ref) => {
  const sheet = useRef<BottomSheet>(null);
  const [walletId, setWalletId] = useState<string>("");
  const [editId, setEditId] = useState<string | undefined>();
  const [type, setType] = useState<TransactionType>("Expense");
  const [amount, setAmount] = useState("0");
  const [categoryId, setCategoryId] = useState<string | undefined>();
  const [note, setNote] = useState("");
  const [date, setDate] = useState(new Date());

  const categories = useCategories(type);
  const upsert = useUpsertTransaction();
  const del = useDeleteTransaction();

  const snapPoints = useMemo(() => ["88%"], []);

  useImperativeHandle(ref, () => ({
    openNew: (wId) => {
      setWalletId(wId);
      setEditId(undefined);
      setType("Expense");
      setAmount("0");
      setCategoryId(undefined);
      setNote("");
      setDate(new Date());
      sheet.current?.expand();
    },
    openEdit: (t) => {
      setWalletId(t.walletId);
      setEditId(t.id);
      setType(t.type);
      setAmount(String(t.amount));
      setCategoryId(t.categoryId ?? undefined);
      setNote(t.note ?? "");
      setDate(t.date ? fromApiDate(t.date) : new Date());
      sheet.current?.expand();
    },
  }));

  const press = (k: string) =>
    setAmount((a) => {
      if (k === "del") return a.length <= 1 ? "0" : a.slice(0, -1);
      if (k === "." && a.includes(".")) return a;
      return a === "0" && k !== "." ? k : a + k;
    });

  const save = async () => {
    const parsed = parseFloat(amount);
    if (!categoryId || !parsed || parsed <= 0) return;
    const body = {
      walletId,
      amount: parsed,
      type,
      date: toApiDate(date),
      note: note || undefined,
      categoryId,
    };
    await upsert.mutateAsync({ id: editId, body });
    sheet.current?.close();
  };

  const remove = () => {
    if (!editId) return;
    del.mutate(editId);
    sheet.current?.close();
  };

  return (
    <BottomSheet
      ref={sheet}
      index={-1}
      snapPoints={snapPoints}
      enablePanDownToClose
    >
      <BottomSheetView style={styles.container}>
        {/* Income / Expense toggle */}
        <View style={styles.row}>
          {(["Expense", "Income"] as TransactionType[]).map((t) => {
            const active = type === t;
            return (
              <Pressable
                key={t}
                onPress={() => {
                  setType(t);
                  setCategoryId(undefined);
                }}
                style={[
                  styles.toggle,
                  {
                    backgroundColor: active
                      ? t === "Income"
                        ? colors.incomeBg
                        : colors.expenseBg
                      : colors.surface,
                  },
                ]}
              >
                <Text
                  style={{
                    fontWeight: "700",
                    color: t === "Income" ? colors.income : colors.expense,
                  }}
                >
                  {t}
                </Text>
              </Pressable>
            );
          })}
        </View>

        {/* Amount display */}
        <Text style={styles.amount}>{amount}</Text>

        {/* Category grid */}
        <View style={styles.wrapRow}>
          {categories.data?.map((c) => (
            <Pressable
              key={c.id}
              onPress={() => setCategoryId(c.id)}
              style={[
                styles.chip,
                {
                  borderColor:
                    categoryId === c.id ? colors.primary : colors.border,
                },
              ]}
            >
              <Text>
                {c.icon ?? "🏷️"} {c.name}
              </Text>
            </Pressable>
          ))}
        </View>

        <TextInput
          placeholder="Note"
          value={note}
          onChangeText={setNote}
          style={styles.input}
        />

        {/* Keypad */}
        <View style={styles.wrapRow}>
          {KEYS.map((k) => (
            <Pressable key={k} onPress={() => press(k)} style={styles.key}>
              <Text style={styles.keyText}>{k === "del" ? "⌫" : k}</Text>
            </Pressable>
          ))}
        </View>

        <Pressable
          onPress={save}
          disabled={!categoryId || upsert.isPending}
          style={[
            styles.primaryBtn,
            { backgroundColor: categoryId ? colors.primary : "#cbd5e1" },
          ]}
        >
          <Text style={styles.primaryBtnText}>
            {editId ? "Update" : "Save"}
          </Text>
        </Pressable>

        {editId && (
          <Pressable onPress={remove} style={styles.deleteBtn}>
            <Text style={{ color: colors.expense }}>Delete</Text>
          </Pressable>
        )}
      </BottomSheetView>
    </BottomSheet>
  );
});

TransactionSheet.displayName = "TransactionSheet";

const styles = StyleSheet.create({
  container: { padding: spacing.lg, gap: spacing.md },
  row: { flexDirection: "row", gap: spacing.sm },
  toggle: {
    flex: 1,
    padding: spacing.md,
    borderRadius: radius.md,
    alignItems: "center",
  },
  amount: { fontSize: 40, fontWeight: "800", textAlign: "right" },
  wrapRow: { flexDirection: "row", flexWrap: "wrap", gap: spacing.sm },
  chip: { padding: spacing.md, borderRadius: radius.md, borderWidth: 1 },
  input: {
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: radius.md,
    padding: spacing.md,
  },
  key: { width: "33.33%", paddingVertical: 18, alignItems: "center" },
  keyText: { fontSize: 22 },
  primaryBtn: {
    padding: spacing.lg,
    borderRadius: radius.lg,
    alignItems: "center",
  },
  primaryBtnText: { color: colors.white, fontWeight: "700" },
  deleteBtn: { padding: spacing.md, alignItems: "center" },
});
