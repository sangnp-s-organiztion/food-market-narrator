import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { useEffect } from "react";
import {
  BrowserRouter,
  Navigate,
  Route,
  Routes,
  useLocation,
} from "react-router-dom";
import { Toaster as Sonner } from "@/components/ui/sonner";
import { Toaster } from "@/components/ui/toaster";
import { TooltipProvider } from "@/components/ui/tooltip";
import { AuthProvider, useAuth } from "@/contexts/AuthContext";
import { RestaurantProvider } from "@/contexts/RestaurantContext";
import LoginPage from "@/pages/LoginPage";
import SelectRestaurantPage from "@/pages/SelectRestaurantPage";
import DashboardLayout from "@/components/DashboardLayout";
import RestaurantPage from "@/pages/RestaurantPage";
import DishesPage from "@/pages/DishesPage";
import ImagesPage from "@/pages/ImagesPage";
import AudioPage from "@/pages/AudioPage";
import AudioHistoryPage from "@/pages/AudioHistoryPage";
import AccountPage from "@/pages/AccountPage";
import NotFound from "@/pages/NotFound";

const queryClient = new QueryClient();
const TITLE_SUFFIX = "Food Market Narrator Saler";

function getTitleByPath(pathname: string): string {
  if (pathname === "/login") return `Đăng nhập | ${TITLE_SUFFIX}`;
  if (pathname === "/select-restaurant") return `Chọn nhà hàng | ${TITLE_SUFFIX}`;
  if (pathname.startsWith("/dashboard/restaurant")) return `Thông tin nhà hàng | ${TITLE_SUFFIX}`;
  if (pathname.startsWith("/dashboard/dishes")) return `Quản lý món ăn | ${TITLE_SUFFIX}`;
  if (pathname.startsWith("/dashboard/images")) return `Quản lý hình ảnh | ${TITLE_SUFFIX}`;
  if (pathname.startsWith("/dashboard/audio/description")) return `Mô tả âm thanh | ${TITLE_SUFFIX}`;
  if (pathname.startsWith("/dashboard/audio/history")) return `Lịch sử thuyết minh | ${TITLE_SUFFIX}`;
  if (pathname.startsWith("/dashboard/audio")) return `Quản lý âm thanh | ${TITLE_SUFFIX}`;
  if (pathname.startsWith("/dashboard/account")) return `Tài khoản | ${TITLE_SUFFIX}`;
  if (pathname === "/") return TITLE_SUFFIX;
  return `Không tìm thấy trang | ${TITLE_SUFFIX}`;
}

function RouteTitleManager() {
  const location = useLocation();

  useEffect(() => {
    document.title = getTitleByPath(location.pathname);
  }, [location.pathname]);

  return null;
}

function ProtectedRoute({ children }: { children: React.ReactNode }) {
  const { isAuthenticated } = useAuth();
  if (!isAuthenticated) return <Navigate to="/login" replace />;
  return <>{children}</>;
}

function AppRoutes() {
  const { isAuthenticated } = useAuth();

  return (
    <Routes>
      <Route
        path="/login"
        element={isAuthenticated ? <Navigate to="/select-restaurant" replace /> : <LoginPage />}
      />
      <Route
        path="/select-restaurant"
        element={
          <ProtectedRoute>
            <SelectRestaurantPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/dashboard"
        element={
          <ProtectedRoute>
            <DashboardLayout />
          </ProtectedRoute>
        }
      >
        <Route index element={<Navigate to="restaurant" replace />} />
        <Route path="restaurant" element={<RestaurantPage />} />
        <Route path="dishes" element={<DishesPage />} />
        <Route path="images" element={<ImagesPage />} />
        <Route path="audio" element={<Navigate to="description" replace />} />
        <Route path="audio/description" element={<AudioPage />} />
        <Route path="audio/history" element={<AudioHistoryPage />} />
        <Route path="account" element={<AccountPage />} />
      </Route>
      <Route
        path="/"
        element={<Navigate to={isAuthenticated ? "/select-restaurant" : "/login"} replace />}
      />
      <Route path="*" element={<NotFound />} />
    </Routes>
  );
}

const App = () => (
  <QueryClientProvider client={queryClient}>
    <TooltipProvider>
      <Toaster />
      <Sonner />
      <AuthProvider>
        <RestaurantProvider>
          <BrowserRouter>
            <RouteTitleManager />
            <AppRoutes />
          </BrowserRouter>
        </RestaurantProvider>
      </AuthProvider>
    </TooltipProvider>
  </QueryClientProvider>
);

export default App;
