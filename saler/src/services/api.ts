import type {
  Audio,
  Dish,
  Language,
  Restaurant,
  RestaurantImage,
  User,
} from "@/types";

const API_BASE =
  (import.meta.env.VITE_API_BASE_URL as string | undefined) ??
  "http://localhost:5044";

type RequestOptions = RequestInit & { skipJsonContentType?: boolean };

async function request<T>(
  path: string,
  options: RequestOptions = {},
): Promise<T> {
  const url = new URL(path, API_BASE).toString();
  const headers = new Headers(options.headers ?? {});

  if (
    !options.skipJsonContentType &&
    !headers.has("Content-Type") &&
    options.body &&
    !(options.body instanceof FormData)
  ) {
    headers.set("Content-Type", "application/json");
  }

  const response = await fetch(url, {
    ...options,
    headers,
    credentials: "include",
  });

  if (!response.ok) {
    const message = await response.text();
    throw new Error(message || `Request failed: ${response.status}`);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}

function normalizeTime(value: string | null | undefined): string {
  if (!value) return "00:00";
  return value.length >= 5 ? value.slice(0, 5) : value;
}

function normalizeImageUrl(url: string): string {
  if (!url) return "";
  if (/^https?:\/\//i.test(url)) return url;

  // Legacy DB values may store only file names (e.g. chili_bbq.PNG)
  // while new uploads store paths like /maui-images/<file>.
  const normalized = url.replace(/\\/g, "/").trim();
  if (normalized.startsWith("/")) {
    return new URL(normalized, API_BASE).toString();
  }

  return new URL(`/maui-images/${normalized}`, API_BASE).toString();
}

function normalizeAudioUrl(url: string): string {
  if (!url) return "";
  if (/^https?:\/\//i.test(url)) return url;

  const normalized = url.replace(/\\/g, "/").trim();
  if (normalized.startsWith("/")) {
    return new URL(normalized, API_BASE).toString();
  }

  // Keep legacy filename so page-level resolver can append language-specific path.
  return normalized;
}

type ApiRestaurant = {
  restaurantId: string;
  name: string;
  description?: string | null;
  latitude?: number | null;
  longitude?: number | null;
  phone?: string | null;
  address?: string | null;
  isActive: boolean;
  userId: number;
  openTime?: string | null;
  closeTime?: string | null;
  createdAt: string;
};

function mapRestaurant(item: ApiRestaurant): Restaurant {
  return {
    restaurant_id: item.restaurantId,
    name: item.name ?? "",
    description: item.description ?? "",
    latitude: item.latitude ?? 0,
    longitude: item.longitude ?? 0,
    phone: item.phone ?? "",
    address: item.address ?? "",
    is_active: Boolean(item.isActive),
    user_id: item.userId,
    open_time: normalizeTime(item.openTime),
    close_time: normalizeTime(item.closeTime),
    created_at: item.createdAt,
  };
}

type ApiDish = {
  dishId: number;
  name: string;
  price?: number | null;
  description?: string | null;
  restaurantId: string;
  imageId?: number | null;
  imageFileName?: string | null;
  createdAt?: string | null;
};

function mapDish(item: ApiDish): Dish {
  // Build image_url from ImageFileName — backend returns just the filename (e.g. "dish_1.jpg")
  // We prepend /maui-images/ so normalizeImageUrl can resolve it to a full URL
  let image_url: string | undefined;
  if (item.imageFileName) {
    const filename = item.imageFileName.replace(/\\/g, "/").trim();
    image_url = normalizeImageUrl(filename);
  }

  return {
    dish_id: item.dishId,
    name: item.name ?? "",
    price: item.price ?? 0,
    description: item.description ?? "",
    restaurant_id: item.restaurantId,
    image_id: item.imageId ?? null,
    image_url,
    created_at: item.createdAt ?? new Date().toISOString(),
  };
}

type ApiImage = {
  imageId: number;
  restaurantId: string;
  imageUrl: string;
  isPrimary: boolean;
  sortOrder: number;
};

function mapImage(item: ApiImage): RestaurantImage {
  return {
    image_id: item.imageId,
    restaurant_id: item.restaurantId,
    image_url: normalizeImageUrl(item.imageUrl),
    is_primary: Boolean(item.isPrimary),
    sort_order: item.sortOrder,
  };
}

type ApiAudio = {
  audioId: number;
  restaurantId: string;
  languageId: number;
  audioUrl: string;
  version: number;
  isActive: boolean;
  dateGeneration: string;
};

function mapAudio(item: ApiAudio): Audio {
  return {
    audio_id: item.audioId,
    restaurant_id: item.restaurantId,
    language_id: item.languageId,
    audio_url: normalizeAudioUrl(item.audioUrl),
    version: item.version,
    is_active: Boolean(item.isActive),
    date_generation: item.dateGeneration,
  };
}

type ApiLanguage = {
  languageId?: number;
  languageName?: string;
  languageCode?: string;
  name?: string;
  code?: string;
};

function mapLanguage(item: ApiLanguage): Language {
  return {
    language_id: item.languageId ?? 0,
    name: item.languageName ?? item.name ?? "",
    code: item.languageCode ?? item.code ?? "",
  };
}

type LoginResponse = {
  userId: number;
  username: string;
};

export async function loginApi(
  username: string,
  password: string,
): Promise<User> {
  const response = await request<LoginResponse>("/Auth/login", {
    method: "POST",
    body: JSON.stringify({ username, password }),
  });

  return {
    user_id: response.userId,
    username: response.username,
  };
}

export async function getMeApi(): Promise<User> {
  const response = await request<LoginResponse>("/Auth/me", { method: "GET" });
  return {
    user_id: response.userId,
    username: response.username,
  };
}

export async function logoutApi(): Promise<void> {
  await request<{ message: string }>("/Auth/logout", { method: "POST" });
}

export async function getRestaurantsApi(): Promise<Restaurant[]> {
  const data = await request<ApiRestaurant[]>("/Restaurant", { method: "GET" });
  return data.map(mapRestaurant);
}

export async function updateRestaurantApi(
  restaurantId: string,
  restaurant: Restaurant,
): Promise<Restaurant> {
  const payload = {
    name: restaurant.name,
    description: restaurant.description,
    phone: restaurant.phone,
    address: restaurant.address,
    latitude: restaurant.latitude,
    longitude: restaurant.longitude,
    openTime: restaurant.open_time,
    closeTime: restaurant.close_time,
  };

  const data = await request<ApiRestaurant>(`/Restaurant/${restaurantId}`, {
    method: "PATCH",
    body: JSON.stringify(payload),
  });

  return mapRestaurant(data);
}

export async function updateRestaurantStatusApi(
  restaurantId: string,
  isActive: boolean,
): Promise<void> {
  await request<{ message: string }>(`/Restaurant/${restaurantId}/status`, {
    method: "PATCH",
    body: JSON.stringify({ isActive }),
  });
}

export async function getRestaurantDishesApi(
  restaurantId: string,
): Promise<Dish[]> {
  const data = await request<ApiDish[]>(
    `/public/Restaurant/${restaurantId}/dishes`,
    { method: "GET" },
  );
  return data.map(mapDish);
}

export async function createDishApi(
  restaurantId: string,
  payload: Omit<Dish, "dish_id" | "restaurant_id" | "created_at">,
): Promise<Dish> {
  const body = {
    name: payload.name,
    price: payload.price,
    description: payload.description,
    imageId: payload.image_id,
  };

  const data = await request<ApiDish>(`/Restaurant/${restaurantId}/dishes`, {
    method: "POST",
    body: JSON.stringify(body),
  });

  return mapDish(data);
}

export async function updateDishApi(
  dishId: number,
  payload: Omit<Dish, "dish_id" | "restaurant_id" | "created_at">,
): Promise<Dish> {
  const body = {
    name: payload.name,
    price: payload.price,
    description: payload.description,
    imageId: payload.image_id,
  };

  const data = await request<ApiDish>(`/Dishes/${dishId}`, {
    method: "PUT",
    body: JSON.stringify(body),
  });

  return mapDish(data);
}

export async function deleteDishApi(dishId: number): Promise<void> {
  await request<{ message: string }>(`/Dishes/${dishId}`, { method: "DELETE" });
}

export async function getRestaurantImagesApi(
  restaurantId: string,
): Promise<RestaurantImage[]> {
  const data = await request<ApiImage[]>(`/Restaurant/${restaurantId}/images`, {
    method: "GET",
  });
  return data.map(mapImage);
}

export async function uploadRestaurantImageApi(
  restaurantId: string,
  file: File,
  isPrimary = false,
  sortOrder = 0,
): Promise<RestaurantImage> {
  const form = new FormData();
  form.append("file", file);
  form.append("is_primary", String(isPrimary));
  form.append("sort_order", String(sortOrder));

  const data = await request<ApiImage>(`/Restaurant/${restaurantId}/images`, {
    method: "POST",
    body: form,
    skipJsonContentType: true,
  });

  return mapImage(data);
}

export async function deleteImageApi(imageId: number): Promise<void> {
  await request<{ message: string }>(`/Images/${imageId}`, {
    method: "DELETE",
  });
}

export async function setPrimaryImageApi(
  imageId: number,
  isPrimary: boolean,
): Promise<void> {
  await request<{ message: string }>(`/Images/${imageId}/primary`, {
    method: "PATCH",
    body: JSON.stringify({ isPrimary }),
  });
}

export async function reorderImagesApi(
  restaurantId: string,
  items: Array<{ image_id: number; sort_order: number }>,
): Promise<void> {
  await request<{ message: string }>(
    `/Restaurant/${restaurantId}/images/reorder`,
    {
      method: "PATCH",
      body: JSON.stringify({
        items: items.map((x) => ({
          imageId: x.image_id,
          sortOrder: x.sort_order,
        })),
      }),
    },
  );
}

/**
 * Replaces the image file for an existing image.
 * Keeps the original is_primary and sort_order values.
 */
export async function replaceImageApi(
  imageId: number,
  file: File,
): Promise<RestaurantImage> {
  const form = new FormData();
  form.append("file", file);

  const data = await request<ApiImage>(`/Images/${imageId}`, {
    method: "PUT",
    body: form,
    skipJsonContentType: true,
  });

  return mapImage(data);
}

export async function getLanguagesApi(): Promise<Language[]> {
  const data = await request<ApiLanguage[]>("/Language", { method: "GET" });
  return data.map(mapLanguage);
}

export async function getRestaurantAudiosApi(
  restaurantId: string,
): Promise<Audio[]> {
  const data = await request<ApiAudio[]>(
    `/public/Restaurant/${restaurantId}/audios`,
    { method: "GET" },
  );
  return data.map(mapAudio);
}

export async function uploadAudioApi(
  restaurantId: string,
  languageId: number,
  file: File,
): Promise<Audio> {
  const form = new FormData();
  form.append("language_id", String(languageId));
  form.append("file", file);

  const data = await request<ApiAudio>(`/Restaurant/${restaurantId}/audios`, {
    method: "POST",
    body: form,
    skipJsonContentType: true,
  });

  return mapAudio(data);
}

export async function updateAudioActiveApi(
  audioId: number,
  isActive: boolean,
): Promise<void> {
  await request<{ message: string }>(`/Audios/${audioId}/active`, {
    method: "PATCH",
    body: JSON.stringify({ isActive }),
  });
}

export async function deleteAudioApi(audioId: number): Promise<void> {
  await request<{ message: string }>(`/Audios/${audioId}`, {
    method: "DELETE",
  });
}
