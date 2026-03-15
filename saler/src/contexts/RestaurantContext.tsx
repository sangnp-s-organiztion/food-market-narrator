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
    const userRestaurants = allRestaurants.filter((r) => r.user_id === user.user_id);
    setRestaurants(userRestaurants);
  }, [user]);

  useEffect(() => {
    let mounted = true;
    async function load() {
      if (!user) {
        setRestaurants([]);
        return;
      }
      try {
        const allRestaurants = await getRestaurantsApi();
        const data = allRestaurants.filter((r) => r.user_id === user.user_id);
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
