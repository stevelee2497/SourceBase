import React from "react";
import { Pressable, Text } from "react-native";
import { colors, radius, spacing } from "../theme/colors";

export function Chip({
  label,
  active,
  onPress,
}: {
  label: string;
  active: boolean;
  onPress: () => void;
}) {
  return (
    <Pressable
      onPress={onPress}
      style={{
        paddingHorizontal: 14,
        paddingVertical: spacing.sm,
        borderRadius: radius.pill,
        backgroundColor: active ? colors.primary : colors.surface,
      }}
    >
      <Text
        style={{
          color: active ? colors.white : "#334155",
          fontWeight: "600",
          fontSize: 12,
        }}
      >
        {label}
      </Text>
    </Pressable>
  );
}
