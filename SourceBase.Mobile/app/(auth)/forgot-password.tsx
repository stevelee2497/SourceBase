import React, { useState } from "react";
import { View, Text, TextInput, Pressable, ActivityIndicator, StyleSheet } from "react-native";
import { Link, useRouter } from "expo-router";
import { useForm, Controller } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { authApi } from "../../src/api/auth";
import { toErrorResponse } from "../../src/api/client";
import { colors, radius, spacing } from "../../src/theme/colors";

const schema = z.object({ email: z.string().email("Enter a valid email") });
type Form = z.infer<typeof schema>;

export default function ForgotPassword() {
  const router = useRouter();
  const [submitting, setSubmitting] = useState(false);
  const [sent, setSent] = useState(false);
  const {
    control,
    handleSubmit,
    setError,
    formState: { errors },
  } = useForm<Form>({ resolver: zodResolver(schema), defaultValues: { email: "" } });

  const onSubmit = async (v: Form) => {
    setSubmitting(true);
    try {
      await authApi.forgotPassword(v.email);
      setSent(true);
    } catch (e) {
      setError("email", { message: toErrorResponse(e).message ?? "Request failed" });
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <View style={styles.container}>
      <Text style={styles.title}>Reset password</Text>
      {sent ? (
        <>
          <Text style={{ color: colors.textMuted }}>
            If that email exists, a reset link has been sent. Enter the token on the next screen.
          </Text>
          <Pressable onPress={() => router.push("/(auth)/reset-password")} style={styles.btn}>
            <Text style={styles.btnText}>Enter reset token</Text>
          </Pressable>
        </>
      ) : (
        <>
          <Controller control={control} name="email" render={({ field }) => (
            <TextInput placeholder="Email" autoCapitalize="none" keyboardType="email-address"
              value={field.value} onChangeText={field.onChange} style={styles.input} />
          )} />
          {errors.email && <Text style={styles.err}>{errors.email.message}</Text>}
          <Pressable onPress={handleSubmit(onSubmit)} disabled={submitting} style={styles.btn}>
            {submitting ? <ActivityIndicator color={colors.white} /> : <Text style={styles.btnText}>Send reset link</Text>}
          </Pressable>
        </>
      )}
      <Link href="/(auth)/login" style={styles.link}>Back to sign in</Link>
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, justifyContent: "center", padding: spacing.xxl, gap: spacing.md },
  title: { fontSize: 28, fontWeight: "700", marginBottom: spacing.md },
  input: { borderWidth: 1, borderColor: colors.border, borderRadius: radius.md, padding: 14 },
  err: { color: colors.expense },
  btn: { backgroundColor: colors.primary, padding: spacing.lg, borderRadius: radius.md, alignItems: "center" },
  btnText: { color: colors.white, fontWeight: "600" },
  link: { textAlign: "center", color: colors.primary, marginTop: spacing.xs },
});
