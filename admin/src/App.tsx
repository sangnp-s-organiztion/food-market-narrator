import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import {
  BrowserRouter,
  Route,
  Routes,
  Navigate,
  useLocation,
} from "react-router-dom";
import { Toaster as Sonner } from "@/components/ui/sonner";
import { Toaster } from "@/components/ui/toaster";
import { TooltipProvider } from "@/components/ui/tooltip";
import { AuthProvider, useAuth } from "@/contexts/AuthContext";
import LoginPage from "./pages/LoginPage";
import Index from "./pages/Index.tsx";
import NotFound from "./pages/NotFound.tsx";
import RestaurantsPage from "./pages/Restaurants.tsx";
import UsersPage from "./pages/UsersPage.tsx";
import LogsPage from "./pages/LogsPage.tsx";
import TrajectoryPage from "./pages/TrajectoryPage.tsx";
import TranslationBillingPage from "./pages/TranslationBillingPage.tsx";

import { ReactNode, useEffect } from "react";

const queryClient = new QueryClient();

const TITLE_SUFFIX = "Food Market Narrator Admin";

const getTitleByPath = (pathname: string): string => {
  if (pathname === "/login") return `Đăng nhập | ${TITLE_SUFFIX}`;
  if (pathname === "/") return `Tổng quan | ${TITLE_SUFFIX}`;
  if (pathname.startsWith("/restaurants")) {
    return `Quản lý nhà hàng | ${TITLE_SUFFIX}`;
  }
  if (pathname.startsWith("/users"))
    return `Quản lý người dùng | ${TITLE_SUFFIX}`;
  if (pathname.startsWith("/logs"))
    return `Nhật ký hoạt động | ${TITLE_SUFFIX}`;
  if (pathname.startsWith("/trajectory"))
    return `Lộ trình người dùng | ${TITLE_SUFFIX}`;
  if (pathname.startsWith("/translation-billing"))
    return `Billing token dịch | ${TITLE_SUFFIX}`;
  return `Không tìm thấy trang | ${TITLE_SUFFIX}`;
};

const RouteTitleManager = () => {
  const location = useLocation();

  useEffect(() => {
    document.title = getTitleByPath(location.pathname);
  }, [location.pathname]);

  return null;
};

// Shown during auth bootstrap (GET /Auth/me round-trip)
const LoadingScreen = () => (
  <div className="min-h-screen flex items-center justify-center bg-background">
    <div className="flex flex-col items-center gap-3">
      <div className="h-8 w-8 rounded-full border-4 border-primary border-t-transparent animate-spin" />
      <p className="text-sm text-muted-foreground">Đang khởi tạo…</p>
    </div>
  </div>
);

const ProtectedRoute = ({ children }: { children: ReactNode }) => {
  const { isAuthenticated, isLoading, user } = useAuth();
  if (isLoading) return <LoadingScreen />;
  if (!isAuthenticated) return <Navigate to="/login" replace />;
  if (!user || user.role.toLowerCase() !== "admin") {
    return <Navigate to="/login" replace />;
  }
  return <>{children}</>;
};

const AppRoutes = () => {
  const { isAuthenticated, isLoading } = useAuth();

  if (isLoading) return <LoadingScreen />;

  return (
    <Routes>
      <Route
        path="/login"
        element={isAuthenticated ? <Navigate to="/" replace /> : <LoginPage />}
      />
      <Route
        path="/"
        element={
          <ProtectedRoute>
            <Index />
          </ProtectedRoute>
        }
      />
      <Route
        path="/restaurants"
        element={
          <ProtectedRoute>
            <RestaurantsPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/users"
        element={
          <ProtectedRoute>
            <UsersPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/logs"
        element={
          <ProtectedRoute>
            <LogsPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/trajectory"
        element={
          <ProtectedRoute>
            <TrajectoryPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/translation-billing"
        element={
          <ProtectedRoute>
            <TranslationBillingPage />
          </ProtectedRoute>
        }
      />
      <Route path="*" element={<NotFound />} />
    </Routes>
  );
};

const App = () => (
  <QueryClientProvider client={queryClient}>
    <TooltipProvider>
      <Toaster />
      <Sonner />
      <BrowserRouter>
        <RouteTitleManager />
        <AuthProvider>
          <AppRoutes />
        </AuthProvider>
      </BrowserRouter>
    </TooltipProvider>
  </QueryClientProvider>
);

export default App;
