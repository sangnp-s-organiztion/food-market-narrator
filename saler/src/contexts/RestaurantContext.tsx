import React, {
  createContext,
  useContext,
  useState,
  useCallback,
  useMemo,
  useEffect,
} from "react";
import type { Restaurant } from "@/types";
import { getUserRestaurants } from "@/services/api";
import { useAuth } from "@/contexts/AuthContext";

interface RestaurantContextType {
  restaurants: Restaurant[];
  isLoading: boolean;
  selectedRestaurant: Restaurant | null;
  selectRestaurant: (restaurantId: string) => void;
  clearSelection: () => void;
  refreshRestaurants: () => Promise<void>;
  replaceRestaurant: (restaurant: Restaurant) => void;
}

const RestaurantContext = createContext<RestaurantContextType | null>(null);

export function RestaurantProvider({
  children,
}: {
  children: React.ReactNode;
}) {
  const { user } = useAuth();
  const [restaurants, setRestaurants] = useState<Restaurant[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [selectedId, setSelectedId] = useState<string | null>(null);

  const refreshRestaurants = useCallback(async () => {
    if (!user) {
      setRestaurants([]);
      return;
    }

    setIsLoading(true);
    try {
      const data = await getUserRestaurants(user.user_id);
      setRestaurants(data);
    } catch (error) {
      console.error(error);
      setRestaurants([]);
    } finally {
      setIsLoading(false);
    }
  }, [user]);

  useEffect(() => {
    void refreshRestaurants();
  }, [refreshRestaurants]);

  useEffect(() => {
    if (!selectedId || restaurants.some((r) => r.restaurant_id === selectedId))
      return;
    setSelectedId(restaurants[0]?.restaurant_id ?? null);
  }, [restaurants, selectedId]);

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

  const replaceRestaurant = useCallback((restaurant: Restaurant) => {
    setRestaurants((prev) =>
      prev.map((item) =>
        item.restaurant_id === restaurant.restaurant_id ? restaurant : item,
      ),
    );
  }, []);

  return (
    <RestaurantContext.Provider
      value={{
        restaurants,
        isLoading,
        selectedRestaurant,
        selectRestaurant,
        clearSelection,
        refreshRestaurants,
        replaceRestaurant,
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
