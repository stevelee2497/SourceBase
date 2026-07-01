import axios, { AxiosError, AxiosRequestConfig } from "axios";
import Constants from "expo-constants";
import { tokenStore } from "../auth/tokenStore";
import { ErrorResponse } from "../models";

const apiBaseUrl =
  (Constants.expoConfig?.extra?.apiBaseUrl as string) ?? "https://api.quoctran.qzz.io";

export const baseURL = `${apiBaseUrl.replace(/\/$/, "")}/api`;

export const api = axios.create({
  baseURL,
  headers: { "Content-Type": "application/json" },
});

// Let the auth layer react to a hard sign-out (failed refresh).
type Listener = () => void;
let onSignOut: Listener = () => {};
export const setSignOutHandler = (fn: Listener) => {
  onSignOut = fn;
};

// Attach bearer token to every request.
api.interceptors.request.use(async (config) => {
  const { accessToken } = await tokenStore.get();
  if (accessToken) {
    config.headers = config.headers ?? {};
    config.headers.Authorization = `Bearer ${accessToken}`;
  }
  return config;
});

// Single-flight refresh: concurrent 401s share one refresh call.
let refreshing: Promise<boolean> | null = null;

async function tryRefresh(): Promise<boolean> {
  if (!refreshing) {
    refreshing = (async () => {
      try {
        const { refreshToken } = await tokenStore.get();
        if (!refreshToken) return false;
        // Raw axios (no interceptors) to avoid recursion.
        const res = await axios.post(`${baseURL}/auth/refresh`, { token: refreshToken });
        await tokenStore.set(res.data.accessToken, res.data.refreshToken);
        return true;
      } catch {
        return false;
      } finally {
        // Reset after the current microtask so queued callers read the settled value.
        setTimeout(() => {
          refreshing = null;
        }, 0);
      }
    })();
  }
  return refreshing;
}

api.interceptors.response.use(
  (r) => r,
  async (error: AxiosError) => {
    const original = error.config as (AxiosRequestConfig & { _retry?: boolean }) | undefined;
    if (error.response?.status === 401 && original && !original._retry) {
      original._retry = true;
      const ok = await tryRefresh();
      if (ok) {
        const { accessToken } = await tokenStore.get();
        original.headers = {
          ...(original.headers as Record<string, string>),
          Authorization: `Bearer ${accessToken}`,
        };
        return api(original);
      }
      await tokenStore.clear();
      onSignOut();
    }
    return Promise.reject(error);
  }
);

// Normalize a thrown axios error into the server ErrorResponse shape.
export function toErrorResponse(e: unknown): ErrorResponse {
  const err = e as AxiosError<ErrorResponse>;
  return (
    err.response?.data ?? {
      message: "Something went wrong. Please try again.",
    }
  );
}
