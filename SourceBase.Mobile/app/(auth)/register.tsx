import React, { useState } from "react";
import { View, Text, TextInput, Pressable, ActivityIndicator, Alert, StyleSheet } from "react-native";
import { Link, useRouter } from "expo-router";
import { useForm, Controller } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { authApi } from "../../src/api/auth";
import { useAuth } from "../../src/auth/AuthContext";
import { toErrorResponse } from "../../src/api/client";
import { colors, radius, spacing } from "../../src/theme/colors";

const schema = z.object({
  firstName: z.string().optional(),
  lastName: z.string().optional(),
  email: z.string().email("Enter a valid email"),
  password: z.string().min(6, "At least 6 characters"),
});
type Form = z.infer<typeof schema>;

export default function Register() {
  const router = useRouter();
  const { signIn } = useAuth();
  const [submitting, setSubmitting] = useState(false);
  const {
    control,
    handleSubmit,
    setError,
    formState: { errors },
  } = useForm<Form>({
    resolver: zodResolver(schema),
    defaultValues: { firstName: "", lastName: "", email: "", password: "" },
  });

  const onSubmit = async (v: Form) => {
    setSubmitting(true);
    try {
      await authApi.register(v);
      // Try to sign in straight away; if email confirmation is required, route to login.
      try {
        await signIn(v.email, v.password);
      } catch {
        Alert.alert("Account created", "Please confirm your email, then sign in.");
        router.replace("/(auth)/login");
      }
    } catch (e) {
      const err = toErrorResponse(e);
      const fieldErrors = err.errors ?? {};
      const keys = Object.keys(fieldErrors);
      if (keys.length) {
        keys.forEach((k) =>
          setError(k.toLowerCase() as keyof Form, { message: fieldErrors[k][0] })
        );
      } else {
        setError("email", { message: err.message ?? "Registration failed" });
      }
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <View style={styles.container}>
      <Text style={styles.title}>Create account</Text>

      <Controller control={control} name="firstName" render={({ field }) => (
        <TextInput placeholder="First name" value={field.value} onChangeText={field.onChange} style={styles.input} />
      )} />
      <Controller control={control} name="lastName" render={({ field }) => (
        <TextInput placeholder="Last name" value={field.value} onChangeText={field.onChange} style={styles.input} />
      )} />
      <Controller control={control} name="email" render={({ field }) => (
        <TextInput placeholder="Email" autoCapitalize="none" keyboardType="email-address"
          value={field.value} onChangeText={field.onChange} style={styles.input} />
      )} />
      {errors.email && <Text style={styles.err}>{errors.email.message}</Text>}
      <Controller control={control} name="password" render={({ field }) => (
        <TextInput placeholder="Password" secureTextEntry value={field.value} onChangeText={field.onChange} style={styles.input} />
      )} />
      {errors.password && <Text style={styles.err}>{errors.password.message}</Text>}

      <Pressable onPress={handleSubmit(onSubmit)} disabled={submitting} style={styles.btn}>
        {submitting ? <ActivityIndicator color={colors.white} /> : <Text style={styles.btnText}>Sign up</Text>}
      </Pressable>

      <Link href="/(auth)/login" style={styles.link}>Already have an account? Sign in</Link>
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
