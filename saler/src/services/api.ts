import type {
  Audio,
  Dish,
  Language,
  Restaurant,
  RestaurantImage,
  User,
} from "@/types";

const API_BASE_URL = (import.meta.env.VITE_API_BASE_URL ?? "").replace(
  /\/$/,
  "",
);

type HttpMethod = "GET" | "POST" | "PUT" | "PATCH" | "DELETE";

const KNOWN_LANGUAGE_ID_BY_CODE: Record<string, number> = {
  en: 1,
  es: 2,
  fr: 3,
  it: 4,
  de: 5,
};

class ApiError extends Error {
  status: number;

  constructor(message: string, status: number) {
    super(message);
    this.name = "ApiError";
    this.status = status;
  }
}

function asString(value: unknown, fallback = ""): string {
  return typeof value === "string" ? value : fallback;
}

function asNumber(value: unknown, fallback = 0): number {
  if (typeof value === "number" && Number.isFinite(value)) return value;
  if (typeof value === "string") {
    const parsed = Number(value);
    if (Number.isFinite(parsed)) return parsed;
  }
  return fallback;
}

function asBoolean(value: unknown, fallback = false): boolean {
  return typeof value === "boolean" ? value : fallback;
}

function normalizeTime(value: unknown): string {
  if (typeof value !== "string") return "00:00";
  const [h = "00", m = "00"] = value.split(":");
  return `${h.padStart(2, "0")}:${m.padStart(2, "0")}`;
}

function buildUrl(path: string): string {
  return `${API_BASE_URL}${path}`;
}

export function resolveAssetUrl(pathOrUrl: string): string {
  if (!pathOrUrl) return "";
  if (/^https?:\/\//i.test(pathOrUrl)) return pathOrUrl;
  if (!pathOrUrl.startsWith("/")) return pathOrUrl;
  return API_BASE_URL ? `${API_BASE_URL}${pathOrUrl}` : pathOrUrl;
}

async function request<T>(
  method: HttpMethod,
  path: string,
  body?: BodyInit | Record<string, unknown> | null,
): Promise<T> {
  const headers: Record<string, string> = {};
  let payload: BodyInit | undefined;

  if (body instanceof FormData) {
    payload = body;
  } else if (body !== undefined && body !== null) {
    headers["Content-Type"] = "application/json";
    payload = JSON.stringify(body);
  }

  const response = await fetch(buildUrl(path), {
    method,
    credentials: "include",
    headers,
    body: payload,
  });

  const text = await response.text();
  const data = text ? safeParseJson(text) : null;

  if (!response.ok) {
    const message =
      (data &&
        typeof data === "object" &&
        "message" in data &&
        asString(data.message)) ||
      `Request failed with status ${response.status}`;
    throw new ApiError(message, response.status);
  }

  return data as T;
}

function safeParseJson(text: string): unknown {
  try {
    return JSON.parse(text);
  } catch {
    return null;
  }
}

function mapRestaurant(raw: unknown): Restaurant {
  const item = (raw ?? {}) as Record<string, unknown>;
  return {
    restaurant_id: asString(item.restaurantId ?? item.restaurant_id),
    name: asString(item.name),
    description: asString(item.description),
    latitude: asNumber(item.latitude),
    longitude: asNumber(item.longitude),
    phone: asString(item.phone),
    address: asString(item.address),
    is_active: asBoolean(item.isActive ?? item.is_active),
    user_id: asNumber(item.userId ?? item.user_id),
    open_time: normalizeTime(item.openTime ?? item.open_time),
    close_time: normalizeTime(item.closeTime ?? item.close_time),
    created_at: asString(item.createdAt ?? item.created_at),
  };
}

function mapDish(raw: unknown): Dish {
  const item = (raw ?? {}) as Record<string, unknown>;
  return {
    dish_id: asNumber(item.dishId ?? item.dish_id),
    name: asString(item.name),
    price: asNumber(item.price),
    description: asString(item.description),
    restaurant_id: asString(item.restaurantId ?? item.restaurant_id),
    image_id:
      item.imageId === null || item.image_id === null
        ? null
        : asNumber(item.imageId ?? item.image_id),
    created_at: asString(item.createdAt ?? item.created_at),
  };
}

function mapImage(raw: unknown, fallbackRestaurantId = ""): RestaurantImage {
  const item = (raw ?? {}) as Record<string, unknown>;
  return {
    image_id: asNumber(item.imageId ?? item.image_id),
    restaurant_id: asString(
      item.restaurantId ?? item.restaurant_id,
      fallbackRestaurantId,
    ),
    image_url: resolveAssetUrl(asString(item.imageUrl ?? item.image_url)),
    is_primary: asBoolean(item.isPrimary ?? item.is_primary),
    sort_order: asNumber(item.sortOrder ?? item.sort_order),
  };
}

function mapAudio(raw: unknown): Audio {
  const item = (raw ?? {}) as Record<string, unknown>;
  return {
    audio_id: asNumber(item.audioId ?? item.audio_id),
    restaurant_id: asString(item.restaurantId ?? item.restaurant_id),
    language_id: asNumber(item.languageId ?? item.language_id),
    audio_url: resolveAssetUrl(asString(item.audioUrl ?? item.audio_url)),
    version: asNumber(item.version),
    is_active: asBoolean(item.isActive ?? item.is_active),
    date_generation: asString(item.dateGeneration ?? item.date_generation),
  };
}

function mapUser(raw: unknown): User {
  const item = (raw ?? {}) as Record<string, unknown>;
  return {
    user_id: asNumber(item.userId ?? item.user_id),
    username: asString(item.username),
  };
}

export async function login(username: string, password: string): Promise<User> {
  const response = await request<unknown>("POST", "/Auth/login", {
    username,
    password,
  });
  return mapUser(response);
}

export async function logout(): Promise<void> {
  await request("POST", "/Auth/logout");
}

export async function me(): Promise<User> {
  const response = await request<unknown>("GET", "/Auth/me");
  return mapUser(response);
}

export async function getUserRestaurants(
  userId: number,
): Promise<Restaurant[]> {
  const response = await request<unknown[]>(
    "GET",
    `/Users/${userId}/restaurants`,
  );
  return (response ?? []).map(mapRestaurant);
}

export async function updateRestaurant(
  id: string,
  payload: {
    name: string;
    description: string;
    phone: string;
    address: string;
    latitude: number;
    longitude: number;
    open_time: string;
    close_time: string;
  },
): Promise<Restaurant> {
  const response = await request<unknown>("PATCH", `/Restaurant/${id}`, {
    name: payload.name,
    description: payload.description,
    phone: payload.phone,
    address: payload.address,
    latitude: payload.latitude,
    longitude: payload.longitude,
    openTime: `${payload.open_time}:00`,
    closeTime: `${payload.close_time}:00`,
  });
  return mapRestaurant(response);
}

export async function updateRestaurantStatus(
  id: string,
  isActive: boolean,
): Promise<void> {
  await request("PATCH", `/Restaurant/${id}/status`, { isActive });
}

export async function getRestaurantDishes(
  restaurantId: string,
): Promise<Dish[]> {
  const response = await request<unknown[]>(
    "GET",
    `/Restaurant/${restaurantId}/dishes`,
  );
  return (response ?? []).map(mapDish);
}

export async function createDish(
  restaurantId: string,
  payload: {
    name: string;
    price: number;
    description: string;
    image_id: number | null;
  },
): Promise<Dish> {
  const response = await request<unknown>(
    "POST",
    `/Restaurant/${restaurantId}/dishes`,
    {
      name: payload.name,
      price: payload.price,
      description: payload.description,
      imageId: payload.image_id,
    },
  );
  return mapDish(response);
}

export async function updateDish(
  dishId: number,
  payload: {
    name: string;
    price: number;
    description: string;
    image_id: number | null;
  },
): Promise<Dish> {
  const response = await request<unknown>("PUT", `/Dishes/${dishId}`, {
    name: payload.name,
    price: payload.price,
    description: payload.description,
    imageId: payload.image_id,
  });
  return mapDish(response);
}

export async function deleteDish(dishId: number): Promise<void> {
  await request("DELETE", `/Dishes/${dishId}`);
}

export async function getRestaurantImages(
  restaurantId: string,
): Promise<RestaurantImage[]> {
  const response = await request<unknown[]>(
    "GET",
    `/Restaurant/${restaurantId}/images`,
  );
  return (response ?? []).map((item) => mapImage(item, restaurantId));
}

export async function uploadRestaurantImage(
  restaurantId: string,
  file: File,
  options?: { isPrimary?: boolean; sortOrder?: number },
): Promise<RestaurantImage> {
  const formData = new FormData();
  formData.append("file", file);
  formData.append("is_primary", String(Boolean(options?.isPrimary)));
  formData.append("sort_order", String(options?.sortOrder ?? 0));

  const response = await request<unknown>(
    "POST",
    `/Restaurant/${restaurantId}/images`,
    formData,
  );
  return mapImage(response, restaurantId);
}

export async function deleteImage(imageId: number): Promise<void> {
  await request("DELETE", `/Images/${imageId}`);
}

export async function setImagePrimary(
  imageId: number,
  isPrimary: boolean,
): Promise<void> {
  await request("PATCH", `/Images/${imageId}/primary`, { isPrimary });
}

export async function reorderImages(
  restaurantId: string,
  items: Array<{ image_id: number; sort_order: number }>,
): Promise<void> {
  await request("PATCH", `/Restaurant/${restaurantId}/images/reorder`, {
    items: items.map((item) => ({
      imageId: item.image_id,
      sortOrder: item.sort_order,
    })),
  });
}

export async function getRestaurantAudios(
  restaurantId: string,
): Promise<Audio[]> {
  const response = await request<unknown[]>(
    "GET",
    `/Restaurant/${restaurantId}/audios`,
  );
  return (response ?? []).map(mapAudio);
}

export async function uploadRestaurantAudio(
  restaurantId: string,
  languageId: number,
  file: File,
): Promise<Audio> {
  const formData = new FormData();
  formData.append("language_id", String(languageId));
  formData.append("file", file);

  const response = await request<unknown>(
    "POST",
    `/Restaurant/${restaurantId}/audios`,
    formData,
  );
  return mapAudio(response);
}

export async function updateAudioActive(
  audioId: number,
  isActive: boolean,
): Promise<void> {
  await request("PATCH", `/Audios/${audioId}/active`, { isActive });
}

export async function deleteAudio(audioId: number): Promise<void> {
  await request("DELETE", `/Audios/${audioId}`);
}

export async function getLanguages(): Promise<Language[]> {
  const response = await request<unknown[]>("GET", "/Language");
  return (response ?? []).map((raw, index) => {
    const item = (raw ?? {}) as Record<string, unknown>;
    const code = asString(item.languageCode ?? item.code).toLowerCase();

    return {
      language_id: KNOWN_LANGUAGE_ID_BY_CODE[code] ?? index + 1,
      name: asString(item.languageName ?? item.name),
      code,
    };
  });
}

export function isApiError(error: unknown): error is ApiError {
  return error instanceof ApiError;
}
