import { useQuery } from "@tanstack/react-query";
import { useEffect, useMemo, useState } from "react";
import AdminLayout from "@/components/AdminLayout";
import { analyticsApi } from "@/lib/analyticsApi";
import { auditApi } from "@/lib/auditApi";
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

function actionBadge(action: string): { cls: string; meaning: string } {
  const normalized = (action ?? "").toUpperCase();

  if (normalized.startsWith("RESTAURANT_")) {
    return {
      cls: "bg-indigo-100 text-indigo-700 border-indigo-200",
      meaning: "thao tác nhà hàng",
    };
  }

  if (normalized.startsWith("DISH_")) {
    return {
      cls: "bg-amber-100 text-amber-700 border-amber-200",
      meaning: "thao tác món ăn",
    };
  }

  if (normalized.startsWith("IMAGE_")) {
    return {
      cls: "bg-fuchsia-100 text-fuchsia-700 border-fuchsia-200",
      meaning: "thao tác hình ảnh",
    };
  }

  if (normalized.startsWith("AUDIO_")) {
    return {
      cls: "bg-cyan-100 text-cyan-700 border-cyan-200",
      meaning: "thao tác audio",
    };
  }

  if (normalized.startsWith("USER_")) {
    return {
      cls: "bg-teal-100 text-teal-700 border-teal-200",
      meaning: "thao tác người dùng",
    };
  }

  switch (normalized) {
    case "LOGIN":
      return {
        cls: "bg-blue-100 text-blue-700 border-blue-200",
        meaning: "bình thường",
      };
    case "LOGOUT":
      return {
        cls: "bg-slate-100 text-slate-700 border-slate-200",
        meaning: "neutral",
      };
    case "CREATE":
      return {
        cls: "bg-emerald-100 text-emerald-700 border-emerald-200",
        meaning: "tạo dữ liệu",
      };
    case "MOBILE_SYNC":
      return {
        cls: "bg-violet-100 text-violet-700 border-violet-200",
        meaning: "low priority",
      };
    case "ERROR":
      return {
        cls: "bg-red-100 text-red-700 border-red-200",
        meaning: "cần chú ý",
      };
    case "MOBILE_PLAY":
      return {
        cls: "bg-cyan-100 text-cyan-700 border-cyan-200",
        meaning: "mobile playback",
      };
    default:
      return {
        cls: "bg-zinc-100 text-zinc-700 border-zinc-200",
        meaning: "khác",
      };
  }
}

const LogsPage = () => {
  const [audioPage, setAudioPage] = useState(1);
  const [auditPage, setAuditPage] = useState(1);

  const {
    data: auditResponse,
    isLoading: isAuditLoading,
    isError: isAuditError,
    isSuccess: isAuditSuccess,
  } = useQuery({
    queryKey: ["audit-logs", auditPage, PAGE_SIZE],
    queryFn: () =>
      auditApi.getLogs({
        page: auditPage,
        pageSize: PAGE_SIZE,
      }),
    placeholderData: (previousData) => previousData,
    staleTime: 30_000,
    refetchInterval: 30_000,
  });

  const {
    data: activityResponse,
    isLoading: isAudioLoading,
    isError: isAudioError,
    isSuccess: isAudioSuccess,
  } = useQuery({
    queryKey: ["analytics", "recent-activity", audioPage, PAGE_SIZE],
    queryFn: () => analyticsApi.getRecentActivity(audioPage, PAGE_SIZE),
    placeholderData: (previousData) => previousData,
    staleTime: 30_000,
    refetchInterval: 30_000, // auto-refresh every 30s for live-ish feed
  });

  const auditItems = auditResponse?.items ?? [];
  const auditTotalCount = auditResponse?.totalCount ?? 0;
  const auditTotalPages = Math.max(1, Math.ceil(auditTotalCount / PAGE_SIZE));

  const activity = activityResponse?.items ?? [];
  const audioTotalPages = activityResponse?.totalPages ?? 0;
  const audioTotalCount = activityResponse?.totalCount ?? 0;

  useEffect(() => {
    if (isAudioSuccess && audioTotalPages > 0 && audioPage > audioTotalPages) {
      setAudioPage(audioTotalPages);
    }
  }, [audioPage, audioTotalPages, isAudioSuccess]);

  useEffect(() => {
    if (isAuditSuccess && auditPage > auditTotalPages) {
      setAuditPage(auditTotalPages);
    }
  }, [auditPage, auditTotalPages, isAuditSuccess]);

  const audioPageWindow = useMemo(() => {
    if (audioTotalPages <= 0) return [] as number[];
    const start = Math.max(1, audioPage - 2);
    const end = Math.min(audioTotalPages, start + 4);
    const adjustedStart = Math.max(1, end - 4);
    return Array.from(
      { length: end - adjustedStart + 1 },
      (_, i) => adjustedStart + i,
    );
  }, [audioPage, audioTotalPages]);

  const auditPageWindow = useMemo(() => {
    if (auditTotalPages <= 0) return [] as number[];
    const start = Math.max(1, auditPage - 2);
    const end = Math.min(auditTotalPages, start + 4);
    const adjustedStart = Math.max(1, end - 4);
    return Array.from(
      { length: end - adjustedStart + 1 },
      (_, i) => adjustedStart + i,
    );
  }, [auditPage, auditTotalPages]);

  const hasAudioPrev = audioPage > 1;
  const hasAudioNext = audioTotalPages > 0 && audioPage < audioTotalPages;
  const hasAuditPrev = auditPage > 1;
  const hasAuditNext = auditPage < auditTotalPages;

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
          <div className="mb-6">
            <div className="mb-3">
              <h2 className="text-lg font-semibold">Nhật ký hệ thống</h2>
            </div>

            <div className="mb-4 flex flex-wrap items-center gap-2">
              {[
                "LOGIN",
                "LOGOUT",
                "RESTAURANT_UPDATE",
                "DISH_CREATE",
                "IMAGE_UPLOAD",
                "AUDIO_DELETE",
                "USER_UPDATE_STATUS",
                "MOBILE_SYNC",
                "ERROR",
              ].map((action) => {
                const style = actionBadge(action);
                return (
                  <span
                    key={action}
                    className={cn(
                      "inline-flex items-center gap-1 rounded-full border px-2 py-0.5 text-xs font-medium",
                      style.cls,
                    )}
                    title={style.meaning}
                  >
                    {action}
                  </span>
                );
              })}
            </div>

            <table className="data-table">
              <thead>
                <tr>
                  <th>Người dùng</th>
                  <th>Hành động</th>
                  <th>IP</th>
                  <th>Thời gian</th>
                </tr>
              </thead>
              <tbody>
                {isAuditLoading && (
                  <tr>
                    <td
                      colSpan={4}
                      className="text-center py-8 text-muted-foreground"
                    >
                      Đang tải nhật ký hệ thống...
                    </td>
                  </tr>
                )}
                {isAuditError && (
                  <tr>
                    <td
                      colSpan={4}
                      className="text-center py-8 text-destructive"
                    >
                      Không thể tải nhật ký hệ thống.
                    </td>
                  </tr>
                )}
                {!isAuditLoading &&
                  !isAuditError &&
                  auditItems.length === 0 && (
                    <tr>
                      <td
                        colSpan={4}
                        className="text-center py-8 text-muted-foreground"
                      >
                        Chưa có nhật ký hệ thống nào.
                      </td>
                    </tr>
                  )}
                {!isAuditLoading &&
                  !isAuditError &&
                  auditItems.map((item, idx) => (
                    <tr key={`${item.username}-${item.action}-${idx}`}>
                      <td className="font-medium text-xs">
                        {item.username?.toLowerCase() === "mobile"
                          ? "visitor"
                          : item.username}
                      </td>
                      <td className="mono text-xs">
                        <span
                          className={cn(
                            "inline-flex items-center rounded-full border px-2 py-0.5 text-[11px] font-medium",
                            actionBadge(item.action).cls,
                          )}
                          title={actionBadge(item.action).meaning}
                        >
                          {item.action}
                        </span>
                      </td>
                      <td className="mono text-xs text-muted-foreground">
                        {item.ipAddress ?? "-"}
                      </td>
                      <td className="mono text-xs text-muted-foreground whitespace-nowrap">
                        {formatTimestamp(item.createdAt)}
                      </td>
                    </tr>
                  ))}
              </tbody>
            </table>

            {!isAuditLoading && !isAuditError && auditItems.length > 0 && (
              <div className="mt-3 px-1 flex flex-col gap-3">
                <p className="text-xs text-muted-foreground">
                  Hiển thị {auditItems.length} / {auditTotalCount} bản ghi nhật
                  ký hệ thống.
                </p>

                <div className="flex flex-wrap items-center gap-2">
                  <button
                    type="button"
                    className="px-3 py-1.5 rounded-md border text-xs font-medium disabled:opacity-50"
                    disabled={!hasAuditPrev}
                    onClick={() => setAuditPage((p) => Math.max(1, p - 1))}
                  >
                    Trang trước
                  </button>

                  {auditPageWindow.map((p) => (
                    <button
                      key={`audit-${p}`}
                      type="button"
                      className={cn(
                        "px-3 py-1.5 rounded-md border text-xs font-medium",
                        p === auditPage
                          ? "bg-primary text-primary-foreground border-primary"
                          : "hover:bg-muted",
                      )}
                      onClick={() => setAuditPage(p)}
                    >
                      {p}
                    </button>
                  ))}

                  <button
                    type="button"
                    className="px-3 py-1.5 rounded-md border text-xs font-medium disabled:opacity-50"
                    disabled={!hasAuditNext}
                    onClick={() => setAuditPage((p) => p + 1)}
                  >
                    Trang sau
                  </button>

                  <span className="text-xs text-muted-foreground ml-1">
                    Trang {auditPage} / {Math.max(auditTotalPages, 1)}
                  </span>
                </div>
              </div>
            )}
          </div>

          <div className="h-px bg-border mb-6" />

          <div className="mb-3">
            <h2 className="text-lg font-semibold">Nhật ký nghe audio</h2>
          </div>

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
              {isAudioLoading && (
                <tr>
                  <td
                    colSpan={5}
                    className="text-center py-8 text-muted-foreground"
                  >
                    Đang tải…
                  </td>
                </tr>
              )}
              {isAudioError && (
                <tr>
                  <td colSpan={5} className="text-center py-8 text-destructive">
                    Không thể tải nhật ký. Vui lòng thử lại.
                  </td>
                </tr>
              )}
              {!isAudioLoading && !isAudioError && activity.length === 0 && (
                <tr>
                  <td
                    colSpan={5}
                    className="text-center py-8 text-muted-foreground"
                  >
                    Chưa có nhật ký nào.
                  </td>
                </tr>
              )}
              {!isAudioLoading &&
                !isAudioError &&
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

          {!isAudioLoading && !isAudioError && activity.length > 0 && (
            <div className="mt-3 px-1 flex flex-col gap-3">
              <p className="text-xs text-muted-foreground">
                Hiển thị {activity.length} / {audioTotalCount} bản ghi nghe
                audio. Nhật ký tự động cập nhật mỗi 30 giây.
              </p>

              <div className="flex flex-wrap items-center gap-2">
                <button
                  type="button"
                  className="px-3 py-1.5 rounded-md border text-xs font-medium disabled:opacity-50"
                  disabled={!hasAudioPrev}
                  onClick={() => setAudioPage((p) => Math.max(1, p - 1))}
                >
                  Trang trước
                </button>

                {audioPageWindow.map((p) => (
                  <button
                    key={p}
                    type="button"
                    className={cn(
                      "px-3 py-1.5 rounded-md border text-xs font-medium",
                      p === audioPage
                        ? "bg-primary text-primary-foreground border-primary"
                        : "hover:bg-muted",
                    )}
                    onClick={() => setAudioPage(p)}
                  >
                    {p}
                  </button>
                ))}

                <button
                  type="button"
                  className="px-3 py-1.5 rounded-md border text-xs font-medium disabled:opacity-50"
                  disabled={!hasAudioNext}
                  onClick={() => setAudioPage((p) => p + 1)}
                >
                  Trang sau
                </button>

                <span className="text-xs text-muted-foreground ml-1">
                  Trang {audioPage} / {Math.max(audioTotalPages, 1)}
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
