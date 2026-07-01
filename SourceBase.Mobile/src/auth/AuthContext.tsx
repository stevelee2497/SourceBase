import React, {
  createContext,
  useContext,
  useEffect,
  useState,
  useCallback,
} from "react";
import { useRouter, useSegments } from "expo-router";
import { tokenStore } from "./tokenStore";
import { setSignOutHandler } from "../api/client";
import { authApi } from "../api/auth";
import { UserInfo } from "../models";

interface AuthState {
  user: UserInfo | null;
  ready: boolean;
  signIn: (email: string, password: string) => Promise<void>;
  signOut: () => Promise<void>;
  refreshUser: () => Promise<void>;
}

const Ctx = createContext<AuthState>({} as AuthState);
export const useAuth = () => useContext(Ctx);

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [user, setUser] = useState<UserInfo | null>(null);
  const [ready, setReady] = useState(false);

  const loadUser = useCallback(async () => {
    const { accessToken } = await tokenStore.get();
    if (!accessToken) {
      setUser(null);
      return;
    }
    try {
      setUser(await authApi.info());
    } catch {
      setUser(null);
    }
  }, []);

  const signOut = useCallback(async () => {
    try {
      await authApi.logout();
    } catch {
      // ignore network errors on logout
    }
    await tokenStore.clear();
    setUser(null);
  }, []);

  useEffect(() => {
    // Hard sign-out triggered by a failed token refresh in the interceptor.
    setSignOutHandler(() => {
      tokenStore.clear();
      setUser(null);
    });
    (async () => {
      await loadUser();
      setReady(true);
    })();
  }, [loadUser]);

  const signIn = useCallback(
    async (email: string, password: string) => {
      const res = await authApi.login(email, password);
      await tokenStore.set(res.accessToken, res.refreshToken);
      await loadUser();
    },
    [loadUser]
  );

  return (
    <Ctx.Provider value={{ user, ready, signIn, signOut, refreshUser: loadUser }}>
      {children}
    </Ctx.Provider>
  );
}

// Route guard: bounce unauthenticated users to login, authed users out of (auth).
export function useProtectedRoute() {
  const { user, ready } = useAuth();
  const segments = useSegments();
  const router = useRouter();

  useEffect(() => {
    if (!ready) return;
    const inAuthGroup = segments[0] === "(auth)";
    if (!user && !inAuthGroup) {
      router.replace("/(auth)/login");
    } else if (user && inAuthGroup) {
      router.replace("/(tabs)");
    }
  }, [user, ready, segments, router]);
}
