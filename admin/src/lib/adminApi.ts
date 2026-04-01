const API_BASE = import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5044";

async function adminFetch<T>(path: string, options?: RequestInit): Promise<T> {
  const res = await fetch(`${API_BASE}${path}`, {
    credentials: "include",
    headers: { "Content-Type": "application/json", ...options?.headers },
    ...options,
  });

  if (!res.ok) {
    const err = await res
      .json()
      .catch(() => ({ message: `Request failed: ${res.status}` }));
    throw new Error(err.message ?? `HTTP ${res.status}`);
  }

  return res.json() as Promise<T>;
}

// ─── Restaurant types ────────────────────────────────────────────────────────

export interface RestaurantImageResponse {
  imageId: number;
  imageUrl: string;
  isPrimary: boolean;
  sortOrder: number;
}

export interface AudioResponse {
  audioId: number;
  languageId: number;
  languageName: string;
  languageCode: string;
  audioUrl: string;
  version: number;
  isActive: boolean;
  dateGeneration: string;
}

export interface RestaurantResponse {
  restaurantId: string;
  name: string;
  description: string | null;
  latitude: number | null;
  longitude: number | null;
  address: string | null;
  phone: string | null;
  isActive: boolean;
  userId: number;
  openTime: string | null;
  closeTime: string | null;
  createdAt: string;
  images: RestaurantImageResponse[];
  audios: AudioResponse[];
}

export interface UpdateRestaurantRequest {
  name: string;
  description?: string | null;
  phone?: string | null;
  address?: string | null;
  latitude?: number | null;
  longitude?: number | null;
  openTime?: string | null;
  closeTime?: string | null;
}

export interface UpdateStatusRequest {
  isActive: boolean;
}

// ─── User types ──────────────────────────────────────────────────────────────

export interface UserResponse {
  userId: number;
  username: string;
  role: string;
  isActive: boolean;
  createdAt: string;
}

export interface CreateUserRequest {
  username: string;
  password: string;
  role: string;
}

export interface UpdateUserRoleRequest {
  role: string;
}

export interface UpdateUserStatusRequest {
  isActive: boolean;
}

export interface CountResponse {
  count: number;
}

// ─── Restaurant API ──────────────────────────────────────────────────────────

export const restaurantApi = {
  getAll: () => adminFetch<RestaurantResponse[]>("/restaurant"),

  getById: (id: string) =>
    adminFetch<RestaurantResponse>(`/api/restaurant/${encodeURIComponent(id)}`),

  update: (id: string, data: UpdateRestaurantRequest) =>
    adminFetch<RestaurantResponse>(
      `/api/restaurant/${encodeURIComponent(id)}`,
      {
        method: "PATCH",
        body: JSON.stringify(data),
      },
    ),

  updateStatus: (id: string, data: UpdateStatusRequest) =>
    adminFetch<{ message: string }>(
      `/api/restaurant/${encodeURIComponent(id)}/status`,
      {
        method: "PATCH",
        body: JSON.stringify(data),
      },
    ),
};

// ─── User API ────────────────────────────────────────────────────────────────

export const userApi = {
  getAll: () => adminFetch<UserResponse[]>("/api/users"),

  getById: (id: number) => adminFetch<UserResponse>(`/api/users/${id}`),

  create: (data: CreateUserRequest) =>
    adminFetch<UserResponse>("/api/users", {
      method: "POST",
      body: JSON.stringify(data),
    }),

  updateRole: (id: number, data: UpdateUserRoleRequest) =>
    adminFetch<{ message: string }>(`/api/users/${id}/role`, {
      method: "PATCH",
      body: JSON.stringify(data),
    }),

  updateStatus: (id: number, data: UpdateUserStatusRequest) =>
    adminFetch<{ message: string }>(`/api/users/${id}/status`, {
      method: "PATCH",
      body: JSON.stringify(data),
    }),
};

// ─── Admin Stats API ───────────────────────────────────────────────────────

export const adminStatsApi = {
  getRestaurantCount: () =>
    adminFetch<CountResponse>("/api/admin/stats/restaurants/count"),

  getAudioCount: () =>
    adminFetch<CountResponse>("/api/admin/stats/audios/count"),

  getUserCount: () => adminFetch<CountResponse>("/api/admin/stats/users/count"),

  getDishCount: () =>
    adminFetch<CountResponse>("/api/admin/stats/dishes/count"),
};
