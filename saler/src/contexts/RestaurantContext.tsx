import React, { createContext, useContext, useState, useCallback, useMemo } from "react";
import type { Restaurant } from "@/types";
import { getUserRestaurants } from "@/services/mockData";
import { useAuth } from "@/contexts/AuthContext";

interface RestaurantContextType {
  restaurants: Restaurant[];
  selectedRestaurant: Restaurant | null;
  selectRestaurant: (restaurantId: number) => void;
  clearSelection: () => void;
}

const RestaurantContext = createContext<RestaurantContextType | null>(null);

export function RestaurantProvider({ children }: { children: React.ReactNode }) {
  const { user } = useAuth();
  const [selectedId, setSelectedId] = useState<number | null>(null);

  const restaurants = useMemo(() => {
    if (!user) return [];
    return getUserRestaurants(user.user_id);
  }, [user]);

  const selectedRestaurant = useMemo(
    () => restaurants.find((r) => r.restaurant_id === selectedId) ?? null,
    [restaurants, selectedId]
  );

  const selectRestaurant = useCallback((restaurantId: number) => {
    setSelectedId(restaurantId);
  }, []);

  const clearSelection = useCallback(() => {
    setSelectedId(null);
  }, []);

  return (
    <RestaurantContext.Provider value={{ restaurants, selectedRestaurant, selectRestaurant, clearSelection }}>
      {children}
    </RestaurantContext.Provider>
  );
}

export function useRestaurant() {
  const ctx = useContext(RestaurantContext);
  if (!ctx) throw new Error("useRestaurant must be used within RestaurantProvider");
  return ctx;
}
