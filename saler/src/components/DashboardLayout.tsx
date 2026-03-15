import { Outlet, Navigate } from "react-router-dom";
import { SidebarProvider, SidebarTrigger } from "@/components/ui/sidebar";
import { DashboardSidebar } from "@/components/DashboardSidebar";
import { useAuth } from "@/contexts/AuthContext";
import { useRestaurant } from "@/contexts/RestaurantContext";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { Store } from "lucide-react";

export default function DashboardLayout() {
  const { user } = useAuth();
  const { restaurants, selectedRestaurant, selectRestaurant, isLoading } =
    useRestaurant();

  if (isLoading) {
    return (
      <div className="min-h-screen flex items-center justify-center">
        <p className="text-muted-foreground">Đang tải dữ liệu nhà hàng...</p>
      </div>
    );
  }

  // Redirect to selection page if no restaurant selected
  if (!selectedRestaurant) {
    return <Navigate to="/select-restaurant" replace />;
  }

  return (
    <SidebarProvider>
      <div className="min-h-screen flex w-full">
        <DashboardSidebar />
        <div className="flex-1 flex flex-col">
          <header className="h-14 flex items-center border-b bg-card px-4 gap-4">
            <SidebarTrigger />
            {restaurants.length > 1 && (
              <div className="flex items-center gap-2">
                <Store className="w-4 h-4 text-muted-foreground" />
                <Select
                  value={String(selectedRestaurant.restaurant_id)}
                  onValueChange={(val) => selectRestaurant(val)}
                >
                  <SelectTrigger className="w-[220px] h-8 text-sm">
                    <SelectValue placeholder="Chọn nhà hàng" />
                  </SelectTrigger>
                  <SelectContent>
                    {restaurants.map((r) => (
                      <SelectItem
                        key={r.restaurant_id}
                        value={String(r.restaurant_id)}
                      >
                        {r.name}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
            )}
            <div className="flex-1" />
            <span className="text-sm text-muted-foreground">
              {user?.username}
            </span>
          </header>
          <main className="flex-1 p-6 overflow-auto">
            <Outlet />
          </main>
        </div>
      </div>
    </SidebarProvider>
  );
}
