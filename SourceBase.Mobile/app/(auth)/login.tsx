import React, { useState } from "react";
import { View, Text, TextInput, Pressable, ActivityIndicator, StyleSheet } from "react-native";
import { Link } from "expo-router";
import { useForm, Controller } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { useAuth } from "../../src/auth/AuthContext";
import { toErrorResponse } from "../../src/api/client";
import { colors, radius, spacing } from "../../src/theme/colors";

const schema = z.object({
  email: z.string().email("Enter a valid email"),
  password: z.string().min(1, "Password is required"),
});
type Form = z.infer<typeof schema>;

export default function Login() {
  const { signIn } = useAuth();
  const [submitting, setSubmitting] = useState(false);
  const {
    control,
    handleSubmit,
    setError,
    formState: { errors },
  } = useForm<Form>({
    resolver: zodResolver(schema),
    defaultValues: { email: "", password: "" },
  });

  const onSubmit = async (v: Form) => {
    setSubmitting(true);
    try {
      await signIn(v.email, v.password);
    } catch (e) {
      const err = toErrorResponse(e);
      const fieldErrors = err.errors ?? {};
      const keys = Object.keys(fieldErrors);
      if (keys.length) {
        keys.forEach((k) =>
          setError(k.toLowerCase() as keyof Form, { message: fieldErrors[k][0] })
        );
      } else {
        setError("password", { message: err.message ?? "Login failed" });
      }
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <View style={styles.container}>
      <Text style={styles.title}>Welcome back</Text>

      <Controller
        control={control}
        name="email"
        render={({ field }) => (
          <TextInput
            placeholder="Email"
            autoCapitalize="none"
            keyboardType="email-address"
            value={field.value}
            onChangeText={field.onChange}
            style={styles.input}
          />
        )}
      />
      {errors.email && <Text style={styles.err}>{errors.email.message}</Text>}

      <Controller
        control={control}
        name="password"
        render={({ field }) => (
          <TextInput
            placeholder="Password"
            secureTextEntry
            value={field.value}
            onChangeText={field.onChange}
            style={styles.input}
          />
        )}
      />
      {errors.password && <Text style={styles.err}>{errors.password.message}</Text>}

      <Pressable onPress={handleSubmit(onSubmit)} disabled={submitting} style={styles.btn}>
        {submitting ? (
          <ActivityIndicator color={colors.white} />
        ) : (
          <Text style={styles.btnText}>Sign in</Text>
        )}
      </Pressable>

      <Link href="/(auth)/forgot-password" style={styles.link}>
        Forgot password?
      </Link>
      <Link href="/(auth)/register" style={styles.link}>
        Create an account
      </Link>
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, justifyContent: "center", padding: spacing.xxl, gap: spacing.md },
  title: { fontSize: 28, fontWeight: "700", marginBottom: spacing.md },
  input: {
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: radius.md,
    padding: 14,
  },
  err: { color: colors.expense },
  btn: {
    backgroundColor: colors.primary,
    padding: spacing.lg,
    borderRadius: radius.md,
    alignItems: "center",
  },
  btnText: { color: colors.white, fontWeight: "600" },
  link: { textAlign: "center", color: colors.primary, marginTop: spacing.xs },
});
