import type { Restaurant, RestaurantImage, Dish, Language, Audio, User, UserRestaurant } from "@/types";

export const mockUser: User = {
  user_id: 1,
  username: "chef_mario",
};

export const mockUserRestaurants: UserRestaurant[] = [
  { id: 1, user_id: 1, restaurant_id: 1, role: "owner" },
  { id: 2, user_id: 1, restaurant_id: 2, role: "manager" },
];

export const mockRestaurants: Restaurant[] = [
  {
    restaurant_id: 1,
    name: "La Trattoria Bella",
    description: "Nhà hàng Ý chính thống phục vụ mì pasta thủ công và pizza nướng lò củi từ năm 1998.",
    latitude: 21.0285,
    longitude: 105.8542,
    phone: "+84 24 1234 5678",
    address: "123 Phố Huế, Hai Bà Trưng, Hà Nội",
    is_active: true,
    open_time: "08:00",
    close_time: "22:00",
    created_at: "2024-01-15T10:30:00Z",
  },
  {
    restaurant_id: 2,
    name: "Chilli BBQ Hotpot",
    description: "Lẩu nướng phong cách Hàn Quốc với nguyên liệu tươi ngon nhất.",
    latitude: 10.7769,
    longitude: 106.7009,
    phone: "+84 28 9876 5432",
    address: "456 Nguyễn Huệ, Quận 1, TP. Hồ Chí Minh",
    is_active: true,
    open_time: "10:00",
    close_time: "23:00",
    created_at: "2024-03-20T08:00:00Z",
  },
];

// Keep backward compat
export const mockRestaurant = mockRestaurants[0];

export const mockImages: RestaurantImage[] = [
  { image_id: 1, restaurant_id: 1, image_url: "https://images.unsplash.com/photo-1517248135467-4c7edcad34c4?w=600", is_primary: true, sort_order: 1 },
  { image_id: 2, restaurant_id: 1, image_url: "https://images.unsplash.com/photo-1555396273-367ea4eb4db5?w=600", is_primary: false, sort_order: 2 },
  { image_id: 3, restaurant_id: 1, image_url: "https://images.unsplash.com/photo-1414235077428-338989a2e8c0?w=600", is_primary: false, sort_order: 3 },
  { image_id: 4, restaurant_id: 2, image_url: "https://images.unsplash.com/photo-1504674900247-0877df9cc836?w=600", is_primary: true, sort_order: 1 },
];

export const mockDishes: Dish[] = [
  { dish_id: 1, name: "Margherita Pizza", price: 14.99, description: "Pizza cổ điển với mozzarella tươi", restaurant_id: 1, image_id: null, created_at: "2024-02-01T00:00:00Z" },
  { dish_id: 2, name: "Fettuccine Alfredo", price: 18.50, description: "Mì fettuccine thủ công trong sốt kem", restaurant_id: 1, image_id: null, created_at: "2024-02-01T00:00:00Z" },
  { dish_id: 3, name: "Tiramisu", price: 9.99, description: "Món tráng miệng Ý truyền thống", restaurant_id: 1, image_id: null, created_at: "2024-02-01T00:00:00Z" },
  { dish_id: 4, name: "Lẩu Kimchi", price: 25.00, description: "Lẩu kimchi cay nồng kiểu Hàn", restaurant_id: 2, image_id: null, created_at: "2024-04-01T00:00:00Z" },
  { dish_id: 5, name: "Thịt nướng BBQ", price: 30.00, description: "Thịt bò nướng than hoa", restaurant_id: 2, image_id: null, created_at: "2024-04-01T00:00:00Z" },
];

export const mockLanguages: Language[] = [
  { language_id: 1, name: "Tiếng Anh", code: "en" },
  { language_id: 2, name: "Tiếng Tây Ban Nha", code: "es" },
  { language_id: 3, name: "Tiếng Pháp", code: "fr" },
  { language_id: 4, name: "Tiếng Ý", code: "it" },
  { language_id: 5, name: "Tiếng Đức", code: "de" },
];

export const mockAudios: Audio[] = [
  { audio_id: 1, restaurant_id: 1, language_id: 1, audio_url: "/audio/en-description.mp3", version: 2, is_active: true, date_generation: "2024-06-01T00:00:00Z" },
  { audio_id: 2, restaurant_id: 1, language_id: 2, audio_url: "/audio/es-description.mp3", version: 1, is_active: true, date_generation: "2024-05-15T00:00:00Z" },
  { audio_id: 3, restaurant_id: 2, language_id: 1, audio_url: "/audio/en-hotpot.mp3", version: 1, is_active: true, date_generation: "2024-07-01T00:00:00Z" },
];

export function getUserRestaurants(userId: number): Restaurant[] {
  const restaurantIds = mockUserRestaurants
    .filter((ur) => ur.user_id === userId)
    .map((ur) => ur.restaurant_id);
  return mockRestaurants.filter((r) => restaurantIds.includes(r.restaurant_id));
}

export function getRestaurantDishes(restaurantId: number): Dish[] {
  return mockDishes.filter((d) => d.restaurant_id === restaurantId);
}

export function getRestaurantImages(restaurantId: number): RestaurantImage[] {
  return mockImages.filter((i) => i.restaurant_id === restaurantId);
}

export function getRestaurantAudios(restaurantId: number): Audio[] {
  return mockAudios.filter((a) => a.restaurant_id === restaurantId);
}
