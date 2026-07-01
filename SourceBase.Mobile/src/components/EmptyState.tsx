import React from "react";
import { View, Text, Pressable } from "react-native";
import { colors, radius, spacing } from "../theme/colors";

export function EmptyState({
  title,
  subtitle,
  actionLabel,
  onAction,
}: {
  title: string;
  subtitle?: string;
  actionLabel?: string;
  onAction?: () => void;
}) {
  return (
    <View style={{ alignItems: "center", padding: spacing.xxl, gap: spacing.sm }}>
      <Text style={{ fontSize: 16, fontWeight: "700", color: colors.text }}>{title}</Text>
      {subtitle && (
        <Text style={{ color: colors.textMuted, textAlign: "center" }}>{subtitle}</Text>
      )}
      {actionLabel && onAction && (
        <Pressable
          onPress={onAction}
          style={{
            marginTop: spacing.sm,
            backgroundColor: colors.primary,
            paddingHorizontal: spacing.xl,
            paddingVertical: spacing.md,
            borderRadius: radius.lg,
          }}
        >
          <Text style={{ color: colors.white, fontWeight: "700" }}>{actionLabel}</Text>
        </Pressable>
      )}
    </View>
  );
}
