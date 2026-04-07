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

export interface CreateRestaurantRequest {
  name: string;
  userId: number;
  description?: string | null;
  phone?: string | null;
  address?: string | null;
  latitude?: number | null;
  longitude?: number | null;
  openTime?: string | null;
  closeTime?: string | null;
  isActive?: boolean;
}

export interface UpdateStatusRequest {
  isActive: boolean;
}
export interface TourStopResponse {
  stopOrder: number;
  restaurantId: string;
  restaurantName: string;
  latitude: number | null;
  longitude: number | null;
  address: string | null;
  primaryImageUrl: string | null;
}

export interface TourResponse {
  tourId: number;
  name: string;
  shortDescription: string | null;
  description: string | null;
  estimatedDurationMinutes: number | null;
  imageUrl: string | null;
  isFeatured: boolean;
  sortPriority: number;
  stopCount: number;
  nearbyStopCount: number;
  nearestDistanceMeters: number | null;
  stops: TourStopResponse[];
}

export interface AddTourRestaurantRequest {
  restaurantId: string;
}

// ─── User types ──────────────────────────────────────────────────────────────

export interface UserResponse {
  userId: number;
  username: string;
  phone?: string | null;
  email?: string | null;
  role: string;
  isActive: boolean;
  createdAt: string;
}

export interface CreateUserRequest {
  username: string;
  password: string;
  phone: string;
  email: string;
  role: string;
}

export interface UpdateUserRoleRequest {
  role: string;
}

export interface UpdateUserStatusRequest {
  isActive: boolean;
}

export interface UpdateUserPasswordRequest {
  oldPassword: string;
  newPassword: string;
}

export interface UpdateMyProfileRequest {
  username: string;
  phone: string;
  email: string;
}

export interface CountResponse {
  count: number;
}

export interface TranslationMonthlyBillingItem {
  sellerUserId: number;
  sellerUsername: string;
  billingMonth: string;
  totalRequests: number;
  successRequests: number;
  failedRequests: number;
  totalBillableUnits: number;
  totalAmount: number;
  currency: string;
  lastRecomputedAtUtc: string;
}

export interface TranslationMonthlyBillingSummary {
  billingMonth: string;
  sellerCount: number;
  totalRequests: number;
  successRequests: number;
  failedRequests: number;
  totalBillableUnits: number;
  totalAmount: number;
  currency: string;
}

export interface TranslationMonthlyBillingResponse {
  items: TranslationMonthlyBillingItem[];
  totalCount: number;
  page: number;
  pageSize: number;
  summary: TranslationMonthlyBillingSummary;
}

export interface TranslationUsageLedgerItem {
  usageEventId: string;
  requestId: string;
  sellerUserId: number;
  sellerUsername: string;
  restaurantId: string;
  audioId: number | null;
  provider: string;
  actionType: string;
  unitType: string;
  inputChars: number;
  outputChars: number;
  billableUnits: number;
  costAmount: number;
  taxAmount: number;
  totalAmount: number;
  currency: string;
  status: string;
  billingMonth: string;
  createdAtUtc: string;
}

export interface TranslationUsageLedgerSummary {
  billingMonth: string;
  status: string;
  eventCount: number;
  totalBillableUnits: number;
  totalAmount: number;
  currency: string;
}

export interface TranslationUsageLedgerResponse {
  items: TranslationUsageLedgerItem[];
  totalCount: number;
  page: number;
  pageSize: number;
  summary: TranslationUsageLedgerSummary;
}

// ─── Restaurant API ──────────────────────────────────────────────────────────

export const restaurantApi = {
  getAll: () => adminFetch<RestaurantResponse[]>("/restaurant"),

  create: (data: CreateRestaurantRequest) =>
    adminFetch<RestaurantResponse>("/restaurant", {
      method: "POST",
      body: JSON.stringify(data),
    }),

  getById: (id: string) =>
    adminFetch<RestaurantResponse>(`/restaurant/${encodeURIComponent(id)}`),

  update: (id: string, data: UpdateRestaurantRequest) =>
    adminFetch<RestaurantResponse>(`/restaurant/${encodeURIComponent(id)}`, {
      method: "PATCH",
      body: JSON.stringify(data),
    }),

  updateStatus: (id: string, data: UpdateStatusRequest) =>
    adminFetch<{ message: string }>(
      `/restaurant/${encodeURIComponent(id)}/status`,
      {
        method: "PATCH",
        body: JSON.stringify(data),
      },
    ),
};

// ─── User API ────────────────────────────────────────────────────────────────

export const tourApi = {
  getAll: () => adminFetch<TourResponse[]>("/Tour"),

  getById: (id: number) => adminFetch<TourResponse>(`/Tour/${id}`),

  addRestaurant: (id: number, data: AddTourRestaurantRequest) =>
    adminFetch<{ message: string }>(`/Tour/${id}/restaurants`, {
      method: "POST",
      body: JSON.stringify(data),
    }),
};
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

  updateMyPassword: (data: UpdateUserPasswordRequest) =>
    adminFetch<{ message: string }>("/Auth/password", {
      method: "PATCH",
      body: JSON.stringify(data),
    }),

  updateMyProfile: (data: UpdateMyProfileRequest) =>
    adminFetch<UserResponse>("/Auth/profile", {
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

type TranslationBillingFilter = {
  billingMonth?: string;
  sellerUserId?: number;
  page?: number;
  pageSize?: number;
};

const toQueryString = (params: Record<string, string | number | undefined>) => {
  const query = new URLSearchParams();
  Object.entries(params).forEach(([key, value]) => {
    if (value !== undefined && value !== null && `${value}`.trim().length > 0) {
      query.set(key, `${value}`);
    }
  });
  return query.toString();
};

export const translationBillingApi = {
  getMonthly: (filter: TranslationBillingFilter) => {
    const query = toQueryString({
      billingMonth: filter.billingMonth,
      sellerUserId: filter.sellerUserId,
      page: filter.page ?? 1,
      pageSize: filter.pageSize ?? 20,
    });

    return adminFetch<TranslationMonthlyBillingResponse>(
      `/api/admin/translation-billing/monthly?${query}`,
    );
  },

  getUsage: (
    filter: TranslationBillingFilter & { status?: "billable" | "failed" },
  ) => {
    const query = toQueryString({
      billingMonth: filter.billingMonth,
      sellerUserId: filter.sellerUserId,
      status: filter.status,
      page: filter.page ?? 1,
      pageSize: filter.pageSize ?? 20,
    });

    return adminFetch<TranslationUsageLedgerResponse>(
      `/api/admin/translation-billing/usage?${query}`,
    );
  },
};

