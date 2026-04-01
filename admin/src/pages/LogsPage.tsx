import { useQuery } from "@tanstack/react-query";
import { useEffect, useMemo, useState } from "react";
import AdminLayout from "@/components/AdminLayout";
import { analyticsApi } from "@/lib/analyticsApi";
import { cn } from "@/lib/utils";

const PAGE_SIZE = 10;

// Map action type from duration-based heuristics
function inferAction(duration: number): { label: string; cls: string } {
  if (duration >= 120)
    return { label: "NGHE ĐẦY ĐỦ", cls: "bg-emerald-100 text-emerald-700" };
  if (duration >= 60)
    return { label: "NGHE TỪNG PHẦN", cls: "bg-blue-100 text-blue-700" };
  if (duration >= 20)
    return { label: "NGHE NHANH", cls: "bg-amber-100 text-amber-700" };
  return { label: "NGẮT SỚM", cls: "bg-red-100 text-red-700" };
}

function formatDuration(seconds: number): string {
  if (seconds < 60) return `${seconds}s`;
  const m = Math.floor(seconds / 60);
  const s = seconds % 60;
  return s > 0 ? `${m}m ${s}s` : `${m}m`;
}

function formatTimestamp(iso: string): string {
  try {
    return new Date(iso).toLocaleString("vi-VN", {
      day: "2-digit",
      month: "2-digit",
      year: "numeric",
      hour: "2-digit",
      minute: "2-digit",
      second: "2-digit",
    });
  } catch {
    return iso;
  }
}

const LogsPage = () => {
  const [page, setPage] = useState(1);

  const {
    data: activityResponse,
    isLoading,
    isError,
  } = useQuery({
    queryKey: ["analytics", "recent-activity", page, PAGE_SIZE],
    queryFn: () => analyticsApi.getRecentActivity(page, PAGE_SIZE),
    staleTime: 30_000,
    refetchInterval: 30_000, // auto-refresh every 30s for live-ish feed
  });

  const activity = activityResponse?.items ?? [];
  const totalPages = activityResponse?.totalPages ?? 0;
  const totalCount = activityResponse?.totalCount ?? 0;

  useEffect(() => {
    if (totalPages > 0 && page > totalPages) {
      setPage(totalPages);
    }
  }, [page, totalPages]);

  const pageWindow = useMemo(() => {
    if (totalPages <= 0) return [] as number[];
    const start = Math.max(1, page - 2);
    const end = Math.min(totalPages, start + 4);
    const adjustedStart = Math.max(1, end - 4);
    return Array.from(
      { length: end - adjustedStart + 1 },
      (_, i) => adjustedStart + i,
    );
  }, [page, totalPages]);

  const hasPrev = page > 1;
  const hasNext = totalPages > 0 && page < totalPages;

  return (
    <AdminLayout>
      <div className="page-header">
        <h1 className="page-title">Nhật ký hoạt động</h1>
        <span className="text-xs text-muted-foreground mono">
          Tự động cập nhật mỗi 30 giây
        </span>
      </div>

      <div className="max-w-7xl mx-auto px-8 py-6">
        <div className="stat-card">
          {/* Subtle hint for action types */}
          <div className="flex flex-wrap gap-3 mb-4">
            {[
              {
                label: "Nghe đầy đủ (≥2p)",
                cls: "bg-emerald-100 text-emerald-700",
              },
              {
                label: "Nghe từng phần (1–2p)",
                cls: "bg-blue-100 text-blue-700",
              },
              {
                label: "Nghe nhanh (20s–1p)",
                cls: "bg-amber-100 text-amber-700",
              },
              { label: "Ngắt sớm (<20s)", cls: "bg-red-100 text-red-700" },
            ].map(({ label, cls }) => (
              <span
                key={label}
                className={cn(
                  "inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium",
                  cls,
                )}
              >
                {label}
              </span>
            ))}
          </div>

          <table className="data-table">
            <thead>
              <tr>
                <th>Nhà hàng</th>
                <th>Audio ID</th>
                <th>Thời lượng</th>
                <th>Hành động</th>
                <th>Thời gian</th>
              </tr>
            </thead>
            <tbody>
              {isLoading && (
                <tr>
                  <td
                    colSpan={5}
                    className="text-center py-8 text-muted-foreground"
                  >
                    Đang tải…
                  </td>
                </tr>
              )}
              {isError && (
                <tr>
                  <td colSpan={5} className="text-center py-8 text-destructive">
                    Không thể tải nhật ký. Vui lòng thử lại.
                  </td>
                </tr>
              )}
              {!isLoading && !isError && activity.length === 0 && (
                <tr>
                  <td
                    colSpan={5}
                    className="text-center py-8 text-muted-foreground"
                  >
                    Chưa có nhật ký nào.
                  </td>
                </tr>
              )}
              {!isLoading &&
                !isError &&
                activity.map((item, idx) => {
                  const action = inferAction(item.duration);
                  return (
                    <tr key={`${item.audioId}-${idx}`}>
                      <td className="font-medium text-xs">
                        {item.restaurantName ?? item.restaurantId}
                      </td>
                      <td className="mono text-xs text-muted-foreground">
                        #{item.audioId}
                      </td>
                      <td className="mono text-xs">
                        {formatDuration(item.duration)}
                      </td>
                      <td>
                        <span
                          className={cn(
                            "inline-block px-2 py-0.5 rounded-full text-xs font-medium",
                            action.cls,
                          )}
                        >
                          {action.label}
                        </span>
                      </td>
                      <td className="mono text-xs text-muted-foreground whitespace-nowrap">
                        {formatTimestamp(item.timestamp)}
                      </td>
                    </tr>
                  );
                })}
            </tbody>
          </table>

          {!isLoading && !isError && activity.length > 0 && (
            <div className="mt-3 px-1 flex flex-col gap-3">
              <p className="text-xs text-muted-foreground">
                Hiển thị {activity.length} / {totalCount} bản ghi. Nhật ký tự
                động cập nhật mỗi 30 giây.
              </p>

              <div className="flex flex-wrap items-center gap-2">
                <button
                  type="button"
                  className="px-3 py-1.5 rounded-md border text-xs font-medium disabled:opacity-50"
                  disabled={!hasPrev}
                  onClick={() => setPage((p) => Math.max(1, p - 1))}
                >
                  Trang trước
                </button>

                {pageWindow.map((p) => (
                  <button
                    key={p}
                    type="button"
                    className={cn(
                      "px-3 py-1.5 rounded-md border text-xs font-medium",
                      p === page
                        ? "bg-primary text-primary-foreground border-primary"
                        : "hover:bg-muted",
                    )}
                    onClick={() => setPage(p)}
                  >
                    {p}
                  </button>
                ))}

                <button
                  type="button"
                  className="px-3 py-1.5 rounded-md border text-xs font-medium disabled:opacity-50"
                  disabled={!hasNext}
                  onClick={() => setPage((p) => p + 1)}
                >
                  Trang sau
                </button>

                <span className="text-xs text-muted-foreground ml-1">
                  Trang {page} / {Math.max(totalPages, 1)}
                </span>
              </div>
            </div>
          )}
        </div>
      </div>
    </AdminLayout>
  );
};

export default LogsPage;
