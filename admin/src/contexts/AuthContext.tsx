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
  refreshMe: () => Promise<void>;
  isLoading: boolean;
}

const AuthContext = createContext<AuthContextType>({} as AuthContextType);
export const useAuth = () => useContext(AuthContext);

export const AuthProvider = ({ children }: { children: ReactNode }) => {
  const [isAuthenticated, setIsAuthenticated] = useState(false);
  const [user, setUser] = useState<LoginResponse | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  const refreshMe = useCallback(async () => {
    const me: MeResponse = await authApi.getMe();
    setUser({
      userId: me.userId,
      username: me.username,
      role: me.role,
      isActive: true,
    });
    setIsAuthenticated(true);
  }, []);

  useEffect(() => {
    refreshMe()
      .catch(() => {
        setIsAuthenticated(false);
        setUser(null);
      })
      .finally(() => setIsLoading(false));
  }, [refreshMe]);

  const login = useCallback(async (username: string, password: string) => {
    const res = await authApi.login({ username, password });
    setUser(res);
    setIsAuthenticated(true);
  }, []);

  const logout = useCallback(async () => {
    await authApi.logout().catch(() => undefined);
    setUser(null);
    setIsAuthenticated(false);
  }, []);

  return (
    <AuthContext.Provider value={{ isAuthenticated, user, login, logout, refreshMe, isLoading }}>
      {children}
    </AuthContext.Provider>
  );
};
