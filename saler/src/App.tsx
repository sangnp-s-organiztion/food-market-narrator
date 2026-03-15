import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { BrowserRouter, Route, Routes, Navigate } from "react-router-dom";
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
import NotFound from "@/pages/NotFound";

const queryClient = new QueryClient();

function ProtectedRoute({ children }: { children: React.ReactNode }) {
  const { isAuthenticated, isInitializing } = useAuth();
  if (isInitializing) return null;
  if (!isAuthenticated) return <Navigate to="/login" replace />;
  return <>{children}</>;
}

function AppRoutes() {
  const { isAuthenticated, isInitializing } = useAuth();

  if (isInitializing) {
    return null;
  }

  return (
    <Routes>
      <Route
        path="/login"
        element={
          isAuthenticated ? (
            <Navigate to="/select-restaurant" replace />
          ) : (
            <LoginPage />
          )
        }
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
        <Route path="audio" element={<AudioPage />} />
      </Route>
      <Route
        path="/"
        element={
          <Navigate
            to={isAuthenticated ? "/select-restaurant" : "/login"}
            replace
          />
        }
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
            <AppRoutes />
          </BrowserRouter>
        </RestaurantProvider>
      </AuthProvider>
    </TooltipProvider>
  </QueryClientProvider>
);

export default App;
