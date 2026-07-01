import React, { useEffect, useState } from "react";
import { View, Text, ActivityIndicator, Pressable, StyleSheet } from "react-native";
import { Link, useLocalSearchParams } from "expo-router";
import { authApi } from "../../src/api/auth";
import { toErrorResponse } from "../../src/api/client";
import { colors, radius, spacing } from "../../src/theme/colors";

// Deep-linked from a confirmation email: jupiter://confirm-email?userId=...&token=...
export default function ConfirmEmail() {
  const { userId, token } = useLocalSearchParams<{ userId?: string; token?: string }>();
  const [state, setState] = useState<"pending" | "ok" | "error">("pending");
  const [message, setMessage] = useState("");

  useEffect(() => {
    (async () => {
      if (!userId || !token) {
        setState("error");
        setMessage("Missing confirmation parameters.");
        return;
      }
      try {
        await authApi.confirmEmail({ userId, token });
        setState("ok");
      } catch (e) {
        setState("error");
        setMessage(toErrorResponse(e).message ?? "Confirmation failed.");
      }
    })();
  }, [userId, token]);

  return (
    <View style={styles.container}>
      {state === "pending" && (
        <>
          <ActivityIndicator color={colors.primary} />
          <Text style={styles.text}>Confirming your email…</Text>
        </>
      )}
      {state === "ok" && (
        <>
          <Text style={styles.title}>Email confirmed</Text>
          <Text style={styles.text}>Your account is ready to use.</Text>
          <Link href="/(auth)/login" asChild>
            <Pressable style={styles.btn}>
              <Text style={styles.btnText}>Sign in</Text>
            </Pressable>
          </Link>
        </>
      )}
      {state === "error" && (
        <>
          <Text style={styles.title}>Confirmation failed</Text>
          <Text style={styles.text}>{message}</Text>
          <Link href="/(auth)/login" style={styles.link}>Back to sign in</Link>
        </>
      )}
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, justifyContent: "center", alignItems: "center", padding: spacing.xxl, gap: spacing.md },
  title: { fontSize: 24, fontWeight: "700" },
  text: { color: colors.textMuted, textAlign: "center" },
  btn: { backgroundColor: colors.primary, paddingHorizontal: spacing.xl, paddingVertical: spacing.md, borderRadius: radius.md, marginTop: spacing.sm },
  btnText: { color: colors.white, fontWeight: "600" },
  link: { color: colors.primary, marginTop: spacing.sm },
});
