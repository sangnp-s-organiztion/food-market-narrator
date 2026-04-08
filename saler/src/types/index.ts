export interface User {
  user_id: number;
  username: string;
  role: string;
  phone?: string;
  email?: string;
  is_active?: boolean;
  created_at?: string;
}

export interface UserRestaurant {
  id: number;
  user_id: number;
  restaurant_id: string;
  role: "owner" | "manager";
}

export interface Restaurant {
  restaurant_id: string;
  name: string;
  description: string;
  latitude: number;
  longitude: number;
  phone: string;
  address: string;
  is_active: boolean;
  user_id: number;
  open_time: string;
  close_time: string;
  created_at: string;
}

export interface RestaurantImage {
  image_id: number;
  restaurant_id: string;
  image_url: string;
  is_primary: boolean;
  sort_order: number;
}

export interface Dish {
  dish_id: number;
  name: string;
  price: number;
  description: string;
  restaurant_id: string;
  image_id: number | null;
  image_url?: string;
  created_at: string;
}

export interface Language {
  language_id: number;
  name: string;
  code: string;
}

export interface Audio {
  audio_id: number;
  restaurant_id: string;
  language_id: number;
  audio_url: string;
  version: number;
  is_active: boolean;
  date_generation: string;
}

export interface TranslateTextResult {
  request_id: string;
  source_language_code: string;
  target_language_code: string;
  translated_text: string;
  input_chars: number;
  output_chars: number;
  estimated_cost: number;
  currency: string;
}

export interface CreateAudioFromTextResult {
  request_id: string;
  audio_id: number;
  audio_url: string;
  language_code: string;
  voice: string;
  created_at: string;
}

export interface AuthState {
  user: User | null;
  isAuthenticated: boolean;
}
