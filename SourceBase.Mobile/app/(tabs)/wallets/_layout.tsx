import React from "react";
import { Stack } from "expo-router";

export default function WalletsStack() {
  return (
    <Stack>
      <Stack.Screen name="index" options={{ title: "Wallets" }} />
      <Stack.Screen name="[id]" options={{ title: "Wallet" }} />
    </Stack>
  );
}
