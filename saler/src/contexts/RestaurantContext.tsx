import React, {
  createContext,
  useContext,
  useState,
  useCallback,
  useMemo,
  useEffect,
} from "react";
import type { Restaurant } from "@/types";
import { getRestaurantsApi } from "@/services/api";
import { useAuth } from "@/contexts/AuthContext";
import { toast } from "sonner";

interface RestaurantContextType {
  restaurants: Restaurant[];
  selectedRestaurant: Restaurant | null;
  selectRestaurant: (restaurantId: string) => void;
  clearSelection: () => void;
  refreshRestaurants: () => Promise<void>;
}

const RestaurantContext = createContext<RestaurantContextType | null>(null);

export function RestaurantProvider({
  children,
}: {
  children: React.ReactNode;
}) {
  const { user } = useAuth();
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [restaurants, setRestaurants] = useState<Restaurant[]>([]);

  const refreshRestaurants = useCallback(async () => {
    if (!user) {
      setRestaurants([]);
      return;
    }

    const allRestaurants = await getRestaurantsApi();
    const userRestaurants = allRestaurants.filter(
      (r) => r.user_id === user.user_id && r.is_active,
    );
    setRestaurants(userRestaurants);
  }, [user]);

  useEffect(() => {
    let mounted = true;
    async function load() {
      if (!user) {
        setRestaurants([]);
        setSelectedId(null);
        return;
      }
      try {
        const allRestaurants = await getRestaurantsApi();
        const data = allRestaurants.filter(
          (r) => r.user_id === user.user_id && r.is_active,
        );
        if (mounted) setRestaurants(data ?? []);
      } catch {
        if (mounted) setRestaurants([]);
      }
    }
    load();
    return () => {
      mounted = false;
    };
  }, [user]);

  useEffect(() => {
    if (!user) {
      return;
    }

    const intervalId = window.setInterval(() => {
      void refreshRestaurants().catch(() => {
        // Ignore polling errors; next tick may recover.
      });
    }, 30_000);

    return () => {
      window.clearInterval(intervalId);
    };
  }, [user, refreshRestaurants]);

  useEffect(() => {
    if (!user || !selectedId) {
      return;
    }

    const stillAvailable = restaurants.some(
      (r) => r.restaurant_id === selectedId,
    );

    if (stillAvailable) {
      return;
    }

    setSelectedId(null);
    toast.error("Nhà hàng đang quản lý đã bị khóa hoặc không còn khả dụng.");
  }, [restaurants, selectedId, user]);

  const selectedRestaurant = useMemo(
    () => restaurants.find((r) => r.restaurant_id === selectedId) ?? null,
    [restaurants, selectedId],
  );

  const selectRestaurant = useCallback((restaurantId: string) => {
    setSelectedId(restaurantId);
  }, []);

  const clearSelection = useCallback(() => {
    setSelectedId(null);
  }, []);

  return (
    <RestaurantContext.Provider
      value={{
        restaurants,
        selectedRestaurant,
        selectRestaurant,
        clearSelection,
        refreshRestaurants,
      }}
    >
      {children}
    </RestaurantContext.Provider>
  );
}

export function useRestaurant() {
  const ctx = useContext(RestaurantContext);
  if (!ctx)
    throw new Error("useRestaurant must be used within RestaurantProvider");
  return ctx;
}
