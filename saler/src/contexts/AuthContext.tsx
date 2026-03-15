import React, { createContext, useContext, useState, useCallback, useEffect } from "react";
import type { User, AuthState } from "@/types";
import { getMeApi, loginApi, logoutApi } from "@/services/api";

interface AuthContextType extends AuthState {
  login: (username: string, password: string) => Promise<boolean>;
  logout: () => void;
}

const AuthContext = createContext<AuthContextType | null>(null);

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [authState, setAuthState] = useState<AuthState>({
    user: null,
    isAuthenticated: false,
  });

  useEffect(() => {
    let mounted = true;

    async function bootstrapAuth() {
      try {
        const me = await getMeApi();
        if (mounted) {
          setAuthState({ user: me, isAuthenticated: true });
        }
      } catch {
        if (mounted) {
          setAuthState({ user: null, isAuthenticated: false });
        }
      }
    }

    bootstrapAuth();
    return () => {
      mounted = false;
    };
  }, []);

  const login = useCallback(async (username: string, password: string) => {
    try {
      const user = await loginApi(username, password);
      setAuthState({ user, isAuthenticated: true });
      return true;
    } catch {
      return false;
    }
  }, []);

  const logout = useCallback(() => {
    void logoutApi().catch(() => undefined);
    setAuthState({ user: null, isAuthenticated: false });
  }, []);

  return (
    <AuthContext.Provider value={{ ...authState, login, logout }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("useAuth must be used within AuthProvider");
  return ctx;
}
