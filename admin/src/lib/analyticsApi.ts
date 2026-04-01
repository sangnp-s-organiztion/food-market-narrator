import type { EntityCounts, ListensTimeseries } from "@/types/analytics";

const API_BASE = import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5044";

async function analyticsFetch<T>(path: string): Promise<T> {
  const res = await fetch(`${API_BASE}${path}`, {
    credentials: "include",
  });
  if (!res.ok) {
    throw new Error(`Analytics API error ${res.status}: ${res.statusText}`);
  }
  return res.json() as Promise<T>;
}

export const analyticsApi = {
  async getEntityCounts(): Promise<EntityCounts> {
    return analyticsFetch<EntityCounts>("/api/analytics/entity-counts");
  },

  async getListensTimeseries(days = 14): Promise<ListensTimeseries> {
    return analyticsFetch<ListensTimeseries>(
      `/api/analytics/listens-timeseries?days=${days}`
    );
  },
};
