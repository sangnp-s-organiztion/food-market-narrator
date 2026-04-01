export interface EntityCounts {
  totalRestaurants: number;
  totalAudios: number;
  totalUsers: number;
  totalDishes: number;
}

export interface ListenCountItem {
  date: string; // "yyyy-MM-dd"
  listens: number;
}

export interface ListensTimeseries {
  items: ListenCountItem[];
}

export interface AuditLogItem {
  id: number;
  userId: number;
  username: string;
  action: string;
  targetType: string;
  targetId: string | null;
  targetName: string | null;
  details: string | null;
  ipAddress: string | null;
  createdAt: string;
}

export interface AuditLogsResponse {
  items: AuditLogItem[];
  totalCount: number;
  page: number;
  pageSize: number;
}
