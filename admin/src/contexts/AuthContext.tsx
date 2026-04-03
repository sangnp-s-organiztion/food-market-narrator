import {
  createContext,
  useContext,
  useState,
  useCallback,
  useEffect,
  type ReactNode,
} from "react";
import { authApi, type LoginResponse, type MeResponse } from "@/lib/authApi";

interface AuthContextType {
  isAuthenticated: boolean;
  user: LoginResponse | null;
  login: (username: string, password: string) => Promise<void>;
  logout: () => Promise<void>;
  isLoading: boolean;
}

const AuthContext = createContext<AuthContextType>({} as AuthContextType);

export const useAuth = () => useContext(AuthContext);

// Persist auth state in memory only — the cookie is the real source of truth.
// On bootstrap, verify the cookie is still valid via GET /Auth/me.
export const AuthProvider = ({ children }: { children: ReactNode }) => {
  const [isAuthenticated, setIsAuthenticated] = useState(false);
  const [user, setUser] = useState<LoginResponse | null>(null);
  const [isLoading, setIsLoading] = useState(true); // bootstrap phase

  // ── Bootstrap: verify existing cookie session ──────────────────────────────
  useEffect(() => {
    authApi
      .getMe()
      .then((me: MeResponse) => {
        // Cookie is valid — restore session from /Auth/me
        setUser({
          userId: me.userId,
          username: me.username,
          role: me.role,
          isActive: true,
        });
        setIsAuthenticated(true);
      })
      .catch(() => {
        // No valid cookie — stay on login page
        setIsAuthenticated(false);
        setUser(null);
      })
      .finally(() => setIsLoading(false));
  }, []);

  // ── Login ────────────────────────────────────────────────────────────────
  const login = useCallback(async (username: string, password: string) => {
    const res = await authApi.login({ username, password });
    setUser(res);
    setIsAuthenticated(true);
  }, []);

  // ── Logout ──────────────────────────────────────────────────────────────
  const logout = useCallback(async () => {
    await authApi.logout().catch(() => {
      // Best-effort logout — clear local state regardless
    });
    setUser(null);
    setIsAuthenticated(false);
  }, []);

  return (
    <AuthContext.Provider value={{ isAuthenticated, user, login, logout, isLoading }}>
      {children}
    </AuthContext.Provider>
  );
};
