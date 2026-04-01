import type { AuditLogsResponse } from "@/types/analytics";

const API_BASE = import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5044";

async function auditFetch<T>(path: string): Promise<T> {
  const res = await fetch(`${API_BASE}${path}`, {
    credentials: "include",
  });
  if (!res.ok) {
    throw new Error(`Audit API error ${res.status}: ${res.statusText}`);
  }
  return res.json() as Promise<T>;
}

export interface AuditLogFilters {
  page?: number;
  pageSize?: number;
  userId?: number;
  action?: string;
  targetType?: string;
  from?: string;
  to?: string;
}

function buildQuery(filters: AuditLogFilters): string {
  const params = new URLSearchParams();
  if (filters.page) params.set("page", String(filters.page));
  if (filters.pageSize) params.set("pageSize", String(filters.pageSize));
  if (filters.userId) params.set("userId", String(filters.userId));
  if (filters.action) params.set("action", filters.action);
  if (filters.targetType) params.set("targetType", filters.targetType);
  if (filters.from) params.set("from", filters.from);
  if (filters.to) params.set("to", filters.to);
  const qs = params.toString();
  return qs ? `?${qs}` : "";
}

export const auditApi = {
  getLogs(filters: AuditLogFilters = {}): Promise<AuditLogsResponse> {
    return auditFetch<AuditLogsResponse>(`/api/audit-logs${buildQuery(filters)}`);
  },
};
