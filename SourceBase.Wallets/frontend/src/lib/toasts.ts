import { useSyncExternalStore } from "react";
import type { ApiError, Toast } from "./types";

const TOAST_TTL_MS = 8000;
let toasts: Toast[] = [];
const listeners = new Set<() => void>();
const emit = () => listeners.forEach((l) => l());

export const dismissToast = (id: string) => { toasts = toasts.filter((t) => t.id !== id); emit(); };

export const showError = (error: Pick<ApiError, "code" | "message" | "traceId">) => {
  const toast: Toast = { id: crypto.randomUUID(), code: error.code, message: error.message, traceId: error.traceId, createdAt: Date.now() };
  toasts = [...toasts, toast];
  emit();
  setTimeout(() => dismissToast(toast.id), TOAST_TTL_MS);
};

export const useToasts = () => useSyncExternalStore((l) => { listeners.add(l); return () => listeners.delete(l); }, () => toasts);
