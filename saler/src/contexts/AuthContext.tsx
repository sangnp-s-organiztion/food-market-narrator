import React, {
  createContext,
  useContext,
  useState,
  useCallback,
  useEffect,
} from "react";
import type { User, AuthState } from "@/types";
import { getMeApi, loginApi, logoutApi } from "@/services/api";

interface AuthContextType extends AuthState {
  login: (username: string, password: string) => Promise<boolean>;
  logout: () => void;
  refreshMe: () => Promise<void>;
}

const AuthContext = createContext<AuthContextType | null>(null);

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [authState, setAuthState] = useState<AuthState>({
    user: null,
    isAuthenticated: false,
  });

  const refreshMe = useCallback(async () => {
    const me = await getMeApi();
    if (me.role !== "saler") {
      await logoutApi().catch(() => undefined);
      setAuthState({ user: null, isAuthenticated: false });
      throw new Error("Unauthorized role");
    }
    setAuthState({ user: me, isAuthenticated: true });
  }, []);

  useEffect(() => {
    let mounted = true;

    async function bootstrapAuth() {
      try {
        await refreshMe();
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
  }, [refreshMe]);

  const login = useCallback(async (username: string, password: string) => {
    try {
      const user = await loginApi(username, password);
      if (user.role !== "saler") {
        await logoutApi().catch(() => undefined);
        setAuthState({ user: null, isAuthenticated: false });
        return false;
      }

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
    <AuthContext.Provider value={{ ...authState, login, logout, refreshMe }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("useAuth must be used within AuthProvider");
  return ctx;
}
