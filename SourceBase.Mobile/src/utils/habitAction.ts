import { HabitLogAction } from "../models";

export const ACTION_META: Record<
  HabitLogAction,
  { label: string; icon: string; color: string }
> = {
  HabitStarted: { label: "Started", icon: "play-circle-outline", color: "#059669" },
  Dismissed: { label: "Dismissed", icon: "close-circle-outline", color: "#dc2626" },
  Snoozed: { label: "Snoozed", icon: "time-outline", color: "#f59e0b" },
  SuppressedVideo: {
    label: "Suppressed (video)",
    icon: "videocam-off-outline",
    color: "#6366f1",
  },
};

export const ALL_ACTIONS: HabitLogAction[] = [
  "HabitStarted",
  "Dismissed",
  "Snoozed",
  "SuppressedVideo",
];
