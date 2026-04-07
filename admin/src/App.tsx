import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import {
  BrowserRouter,
  Navigate,
  Route,
  Routes,
  useLocation,
} from "react-router-dom";
import { ReactNode, useEffect } from "react";
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
import AccountPage from "./pages/AccountPage.tsx";

const queryClient = new QueryClient();
const TITLE_SUFFIX = "Food Market Narrator Admin";

const getTitleByPath = (pathname: string): string => {
  if (pathname === "/login") return `Dang nhap | ${TITLE_SUFFIX}`;
  if (pathname === "/") return `Tong quan | ${TITLE_SUFFIX}`;
  if (pathname.startsWith("/restaurants")) return `Quan ly nha hang | ${TITLE_SUFFIX}`;
  if (pathname.startsWith("/users")) return `Quan ly nguoi dung | ${TITLE_SUFFIX}`;
  if (pathname.startsWith("/logs")) return `Nhat ky hoat dong | ${TITLE_SUFFIX}`;
  if (pathname.startsWith("/trajectory")) return `Lo trinh nguoi dung | ${TITLE_SUFFIX}`;
  if (pathname.startsWith("/translation-billing")) return `Billing token dich | ${TITLE_SUFFIX}`;
  if (pathname.startsWith("/account")) return `Tai khoan | ${TITLE_SUFFIX}`;
  return `Khong tim thay trang | ${TITLE_SUFFIX}`;
};

const RouteTitleManager = () => {
  const location = useLocation();

  useEffect(() => {
    document.title = getTitleByPath(location.pathname);
  }, [location.pathname]);

  return null;
};

const LoadingScreen = () => (
  <div className="flex min-h-screen items-center justify-center bg-background">
    <div className="flex flex-col items-center gap-3">
      <div className="h-8 w-8 animate-spin rounded-full border-4 border-primary border-t-transparent" />
      <p className="text-sm text-muted-foreground">Dang khoi tao...</p>
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
      <Route
        path="/account"
        element={
          <ProtectedRoute>
            <AccountPage />
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
