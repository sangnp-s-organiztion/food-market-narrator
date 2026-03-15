import { useRestaurant } from "@/contexts/RestaurantContext";
import { useNavigate } from "react-router-dom";
import { Store, MapPin, Phone } from "lucide-react";
import { Button } from "@/components/ui/button";
import { useEffect } from "react";

export default function SelectRestaurantPage() {
  const { restaurants, selectRestaurant, isLoading } = useRestaurant();
  const navigate = useNavigate();

  // Auto-select if only one restaurant
  useEffect(() => {
    if (restaurants.length === 1) {
      selectRestaurant(restaurants[0].restaurant_id);
      navigate("/dashboard/restaurant", { replace: true });
    }
  }, [restaurants, selectRestaurant, navigate]);

  const handleSelect = (restaurantId: string) => {
    selectRestaurant(restaurantId);
    navigate("/dashboard/restaurant", { replace: true });
  };

  if (isLoading) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-background p-4">
        <p className="text-muted-foreground">Đang tải danh sách nhà hàng...</p>
      </div>
    );
  }

  if (restaurants.length === 0) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-background p-4">
        <p className="text-muted-foreground">
          Không có nhà hàng nào được gán cho tài khoản này.
        </p>
      </div>
    );
  }

  if (restaurants.length === 1) return null;

  return (
    <div className="min-h-screen flex items-center justify-center bg-background p-4">
      <div className="w-full max-w-2xl animate-fade-in">
        <div className="text-center mb-8">
          <div className="inline-flex items-center justify-center w-16 h-16 rounded-2xl bg-primary/10 mb-4">
            <Store className="w-8 h-8 text-primary" />
          </div>
          <h1 className="text-3xl font-display font-semibold text-foreground">
            Chọn nhà hàng để quản lý
          </h1>
          <p className="text-muted-foreground mt-2">
            Bạn đang quản lý {restaurants.length} nhà hàng. Chọn một nhà hàng để
            tiếp tục.
          </p>
        </div>

        <div className="grid gap-4">
          {restaurants.map((restaurant) => (
            <div
              key={restaurant.restaurant_id}
              className="form-section flex items-center justify-between gap-4"
            >
              <div className="flex-1 min-w-0">
                <h3 className="font-semibold text-foreground text-lg truncate">
                  {restaurant.name}
                </h3>
                <div className="flex items-center gap-1 text-sm text-muted-foreground mt-1">
                  <MapPin className="w-3.5 h-3.5 shrink-0" />
                  <span className="truncate">{restaurant.address}</span>
                </div>
                <div className="flex items-center gap-1 text-sm text-muted-foreground mt-0.5">
                  <Phone className="w-3.5 h-3.5 shrink-0" />
                  <span>{restaurant.phone}</span>
                </div>
              </div>
              <Button onClick={() => handleSelect(restaurant.restaurant_id)}>
                Quản lý
              </Button>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}
