import React, {
  createContext,
  useContext,
  useState,
  useCallback,
  useEffect,
} from "react";
import type { User, AuthState } from "@/types";
import {
  isApiError,
  login as loginApi,
  logout as logoutApi,
  me,
} from "@/services/api";

interface AuthContextType extends AuthState {
  isInitializing: boolean;
  login: (username: string, password: string) => Promise<boolean>;
  logout: () => Promise<void>;
}

const AuthContext = createContext<AuthContextType | null>(null);

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [authState, setAuthState] = useState<AuthState>({
    user: null,
    isAuthenticated: false,
  });
  const [isInitializing, setIsInitializing] = useState(true);

  useEffect(() => {
    const bootstrap = async () => {
      try {
        const user = await me();
        setAuthState({ user, isAuthenticated: true });
      } catch {
        setAuthState({ user: null, isAuthenticated: false });
      } finally {
        setIsInitializing(false);
      }
    };

    void bootstrap();
  }, []);

  const login = useCallback(async (username: string, password: string) => {
    try {
      const user = await loginApi(username.trim(), password);
      setAuthState({ user, isAuthenticated: true });
      return true;
    } catch (error) {
      if (!isApiError(error)) {
        console.error(error);
      }
      setAuthState({ user: null, isAuthenticated: false });
      return false;
    }
  }, []);

  const logout = useCallback(async () => {
    try {
      await logoutApi();
    } catch {
      // Always clear local auth state even when network call fails.
    }
    setAuthState({ user: null, isAuthenticated: false });
  }, []);

  return (
    <AuthContext.Provider
      value={{ ...authState, isInitializing, login, logout }}
    >
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("useAuth must be used within AuthProvider");
  return ctx;
}
