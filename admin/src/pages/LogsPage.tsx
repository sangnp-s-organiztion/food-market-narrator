import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import AdminLayout from "@/components/AdminLayout";
import { auditApi } from "@/lib/auditApi";
import type { AuditLogItem } from "@/types/analytics";
import { cn } from "@/lib/utils";

const PAGE_SIZE = 20;

const ACTION_LABELS: Record<string, { label: string; cls: string }> = {
  LOGIN:         { label: "Đăng nhập",       cls: "bg-emerald-100 text-emerald-700" },
  LOGOUT:        { label: "Đăng xuất",        cls: "bg-slate-100 text-slate-700" },
  CREATE:        { label: "Tạo mới",          cls: "bg-blue-100 text-blue-700" },
  UPDATE:        { label: "Cập nhật",         cls: "bg-amber-100 text-amber-700" },
  UPDATE_STATUS: { label: "Đổi trạng thái",  cls: "bg-amber-100 text-amber-700" },
  UPDATE_ROLE:   { label: "Đổi quyền",        cls: "bg-purple-100 text-purple-700" },
  DELETE:        { label: "Xóa",              cls: "bg-red-100 text-red-700" },
};

function formatTimestamp(iso: string): string {
  try {
    return new Date(iso).toLocaleString("vi-VN", {
      day: "2-digit", month: "2-digit", year: "numeric",
      hour: "2-digit", minute: "2-digit",
    });
  } catch { return iso; }
}

export default function LogsPage() {
  const [page, setPage] = useState(1);

  const { data, isLoading, isError } = useQuery({
    queryKey: ["audit-logs", page],
    queryFn: () => auditApi.getLogs({ page, pageSize: PAGE_SIZE }),
    staleTime: 30_000,
    refetchInterval: 30_000,
  });

  const totalPages = data ? Math.ceil(data.totalCount / PAGE_SIZE) : 0;

  return (
    <AdminLayout>
      <div className="page-header">
        <h1 className="page-title">Nhật ký hành động Admin</h1>
        <span className="text-xs text-muted-foreground mono">
          Tự động cập nhật mỗi 30 giây
        </span>
      </div>

      <div className="max-w-7xl mx-auto px-8 py-6">
        <div className="stat-card">
          {isLoading && <p className="text-center py-8 text-muted-foreground">Đang tải…</p>}
          {isError && <p className="text-center py-8 text-destructive">Không thể tải nhật ký.</p>}

          {!isLoading && !isError && data && (
            <>
              <table className="data-table">
                <thead>
                  <tr>
                    <th>Thời gian</th>
                    <th>Người dùng</th>
                    <th>Hành động</th>
                    <th>Đối tượng</th>
                    <th>Chi tiết</th>
                  </tr>
                </thead>
                <tbody>
                  {data.items.length === 0 && (
                    <tr>
                      <td colSpan={5} className="text-center py-8 text-muted-foreground">
                        Chưa có nhật ký nào.
                      </td>
                    </tr>
                  )}
                  {data.items.map((log: AuditLogItem) => {
                    const actionInfo = ACTION_LABELS[log.action] ?? {
                      label: log.action,
                      cls: "bg-gray-100 text-gray-700",
                    };
                    return (
                      <tr key={log.id}>
                        <td className="mono text-xs whitespace-nowrap">
                          {formatTimestamp(log.createdAt)}
                        </td>
                        <td className="text-sm">{log.username}</td>
                        <td>
                          <span className={cn("inline-block px-2 py-0.5 rounded-full text-xs font-medium", actionInfo.cls)}>
                            {actionInfo.label}
                          </span>
                        </td>
                        <td className="text-sm">
                          {log.targetType}
                          {log.targetId ? ` #${log.targetId}` : ""}
                          {log.targetName ? ` — ${log.targetName}` : ""}
                        </td>
                        <td className="text-xs text-muted-foreground max-w-[200px] truncate">
                          {log.details ?? "—"}
                        </td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>

              {totalPages > 1 && (
                <div className="flex items-center justify-between mt-4 px-1">
                  <span className="text-xs text-muted-foreground">
                    Trang {page} / {totalPages} — {data.totalCount} bản ghi
                  </span>
                  <div className="flex gap-2">
                    <button
                      className="px-3 py-1 text-sm border rounded disabled:opacity-50"
                      onClick={() => setPage((p) => Math.max(1, p - 1))}
                      disabled={page <= 1}
                    >
                      ← Trước
                    </button>
                    <button
                      className="px-3 py-1 text-sm border rounded disabled:opacity-50"
                      onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
                      disabled={page >= totalPages}
                    >
                      Sau →
                    </button>
                  </div>
                </div>
              )}

              <p className="text-xs text-muted-foreground mt-3 px-1">
                Nhật ký tự động cập nhật mỗi 30 giây.
              </p>
            </>
          )}
        </div>
      </div>
    </AdminLayout>
  );
}
