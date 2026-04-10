const API_BASE = import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5044";

async function adminFetch<T>(path: string, options?: RequestInit): Promise<T> {
  const isFormData = options?.body instanceof FormData;
  const res = await fetch(`${API_BASE}${path}`, {
    credentials: "include",
    headers: {
      ...(isFormData ? {} : { "Content-Type": "application/json" }),
      ...options?.headers,
    },
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
  description: string | null;
  estimatedDurationMinutes: number | null;
  imageUrl: string | null;
  isActive: boolean;
  createdAt: string;
  stopCount: number;
  nearbyStopCount: number;
  nearestDistanceMeters: number | null;
  stops: TourStopResponse[];
}

export interface TourImageUploadResponse {
  imageUrl: string;
}

export interface AddTourRestaurantRequest {
  restaurantId: string;
}

export interface ReorderTourStopsRequest {
  restaurantIds: string[];
}

export interface UpdateTourRequest {
  name?: string | null;
  description?: string | null;
  estimatedDurationMinutes: number | null;
  imageUrl: string | null;
  isActive: boolean;
}

export interface CreateTourRequest {
  name: string;
  description?: string | null;
  estimatedDurationMinutes: number | null;
  imageUrl?: string | null;
  isActive: boolean;
}

function buildCreateTourFormData(data: CreateTourRequest): FormData {
  const formData = new FormData();

  formData.append("name", data.name);

  if (data.description !== null && data.description !== undefined) {
    formData.append("description", data.description);
  }

  if (
    data.estimatedDurationMinutes !== null &&
    data.estimatedDurationMinutes !== undefined
  ) {
    formData.append(
      "estimatedDurationMinutes",
      `${data.estimatedDurationMinutes}`,
    );
  }

  if (data.imageUrl !== null && data.imageUrl !== undefined) {
    formData.append("urlImage", data.imageUrl);
  }

  formData.append("isActive", `${data.isActive}`);

  return formData;
}

function buildUpdateTourFormData(data: UpdateTourRequest): FormData {
  const formData = new FormData();

  if (data.name !== null && data.name !== undefined) {
    formData.append("name", data.name);
  }

  if (data.description !== null && data.description !== undefined) {
    formData.append("description", data.description);
  }

  if (
    data.estimatedDurationMinutes !== null &&
    data.estimatedDurationMinutes !== undefined
  ) {
    formData.append(
      "estimatedDurationMinutes",
      `${data.estimatedDurationMinutes}`,
    );
  }

  if (data.imageUrl !== null && data.imageUrl !== undefined) {
    formData.append("urlImage", data.imageUrl);
  }

  formData.append("isActive", `${data.isActive}`);

  return formData;
}

// ─── User types ──────────────────────────────────────────────────────────────

export interface UserResponse {
  userId: number;
  username: string;
  phone?: string | null;
  email?: string | null;
  fullName?: string | null;
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
  billingMonth: string;
  createdAtUtc: string;
}

export interface TranslationUsageLedgerSummary {
  billingMonth: string;
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

export interface AudioUsageLedgerItem {
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
  billingMonth: string;
  createdAtUtc: string;
}

export interface AudioUsageLedgerSummary {
  billingMonth: string;
  eventCount: number;
  totalBillableUnits: number;
}

export interface AudioUsageLedgerResponse {
  items: AudioUsageLedgerItem[];
  totalCount: number;
  page: number;
  pageSize: number;
  summary: AudioUsageLedgerSummary;
}

export interface ResolvedMapCoordinatesResponse {
  latitude: number;
  longitude: number;
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

  uploadImage: (
    id: string,
    file: File,
    options?: { isPrimary?: boolean; sortOrder?: number },
  ) => {
    const formData = new FormData();
    formData.append("file", file);
    formData.append("is_primary", `${options?.isPrimary ?? true}`);
    formData.append("sort_order", `${options?.sortOrder ?? 1}`);

    return adminFetch<RestaurantImageResponse>(
      `/Restaurant/${encodeURIComponent(id)}/images`,
      {
        method: "POST",
        body: formData,
      },
    );
  },
};

// ─── User API ────────────────────────────────────────────────────────────────

export const tourApi = {
  getAll: () => adminFetch<TourResponse[]>("/Tour"),

  getById: (id: number) => adminFetch<TourResponse>(`/Tour/${id}`),

  uploadImage: (file: File) => {
    const formData = new FormData();
    formData.append("file", file);

    return adminFetch<TourImageUploadResponse>("/Tour/upload-image", {
      method: "POST",
      body: formData,
    });
  },

  uploadImageForTour: (id: number, file: File) => {
    const formData = new FormData();
    formData.append("file", file);

    return adminFetch<TourImageUploadResponse>(`/Tour/${id}/upload-image`, {
      method: "POST",
      body: formData,
    });
  },

  create: (data: CreateTourRequest) =>
    adminFetch<TourResponse>("/Tour", {
      method: "POST",
      body: buildCreateTourFormData(data),
    }),

  addRestaurant: (id: number, data: AddTourRestaurantRequest) =>
    adminFetch<{ message: string }>(`/Tour/${id}/restaurants`, {
      method: "POST",
      body: JSON.stringify(data),
    }),

  removeRestaurant: (id: number, restaurantId: string) =>
    adminFetch<{ message: string }>(
      `/Tour/${id}/restaurants/${encodeURIComponent(restaurantId)}`,
      {
        method: "DELETE",
      },
    ),

  reorderStops: (id: number, data: ReorderTourStopsRequest) =>
    adminFetch<{ message: string }>(`/Tour/${id}/stops/order`, {
      method: "PUT",
      body: JSON.stringify(data),
    }),

  update: (id: number, data: UpdateTourRequest) =>
    adminFetch<{ message: string }>(`/Tour/${id}`, {
      method: "PATCH",
      body: buildUpdateTourFormData(data),
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

  getUsage: (filter: TranslationBillingFilter) => {
    const query = toQueryString({
      billingMonth: filter.billingMonth,
      sellerUserId: filter.sellerUserId,
      page: filter.page ?? 1,
      pageSize: filter.pageSize ?? 20,
    });

    return adminFetch<TranslationUsageLedgerResponse>(
      `/api/admin/translation-billing/usage?${query}`,
    );
  },

  getAudioUsage: (filter: TranslationBillingFilter) => {
    const query = toQueryString({
      billingMonth: filter.billingMonth,
      sellerUserId: filter.sellerUserId,
      page: filter.page ?? 1,
      pageSize: filter.pageSize ?? 20,
    });

    return adminFetch<AudioUsageLedgerResponse>(
      `/api/admin/translation-billing/audio-usage?${query}`,
    );
  },
};

export const mapsApi = {
  resolveCoordinates: (url: string) =>
    adminFetch<ResolvedMapCoordinatesResponse>(
      `/api/maps/resolve-coordinates?url=${encodeURIComponent(url)}`,
    ),
};
