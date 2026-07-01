import React, { useState } from "react";
import { View, Text, TextInput, Pressable, ActivityIndicator, Alert, StyleSheet } from "react-native";
import { Link, useRouter, useLocalSearchParams } from "expo-router";
import { useForm, Controller } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { authApi } from "../../src/api/auth";
import { toErrorResponse } from "../../src/api/client";
import { colors, radius, spacing } from "../../src/theme/colors";

const schema = z.object({
  email: z.string().email("Enter a valid email"),
  token: z.string().min(1, "Reset token is required"),
  password: z.string().min(6, "At least 6 characters"),
});
type Form = z.infer<typeof schema>;

export default function ResetPassword() {
  const router = useRouter();
  const params = useLocalSearchParams<{ email?: string; token?: string }>();
  const [submitting, setSubmitting] = useState(false);
  const {
    control,
    handleSubmit,
    setError,
    formState: { errors },
  } = useForm<Form>({
    resolver: zodResolver(schema),
    defaultValues: {
      email: params.email ?? "",
      token: params.token ?? "",
      password: "",
    },
  });

  const onSubmit = async (v: Form) => {
    setSubmitting(true);
    try {
      await authApi.resetPassword(v);
      Alert.alert("Password reset", "You can now sign in with your new password.");
      router.replace("/(auth)/login");
    } catch (e) {
      setError("password", { message: toErrorResponse(e).message ?? "Reset failed" });
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <View style={styles.container}>
      <Text style={styles.title}>Set new password</Text>

      <Controller control={control} name="email" render={({ field }) => (
        <TextInput placeholder="Email" autoCapitalize="none" keyboardType="email-address"
          value={field.value} onChangeText={field.onChange} style={styles.input} />
      )} />
      {errors.email && <Text style={styles.err}>{errors.email.message}</Text>}
      <Controller control={control} name="token" render={({ field }) => (
        <TextInput placeholder="Reset token" autoCapitalize="none"
          value={field.value} onChangeText={field.onChange} style={styles.input} />
      )} />
      {errors.token && <Text style={styles.err}>{errors.token.message}</Text>}
      <Controller control={control} name="password" render={({ field }) => (
        <TextInput placeholder="New password" secureTextEntry
          value={field.value} onChangeText={field.onChange} style={styles.input} />
      )} />
      {errors.password && <Text style={styles.err}>{errors.password.message}</Text>}

      <Pressable onPress={handleSubmit(onSubmit)} disabled={submitting} style={styles.btn}>
        {submitting ? <ActivityIndicator color={colors.white} /> : <Text style={styles.btnText}>Reset password</Text>}
      </Pressable>
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
