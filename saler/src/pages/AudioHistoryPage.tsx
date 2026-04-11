import { useMemo, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { History } from "lucide-react";
import { Input } from "@/components/ui/input";
import { getMyTranslationUsageApi, getRestaurantKpisApi } from "@/services/api";
import { useRestaurant } from "@/contexts/RestaurantContext";

const PAGE_SIZE = 20;

const getCurrentMonth = () => {
  const now = new Date();
  const yyyy = now.getFullYear();
  const mm = `${now.getMonth() + 1}`.padStart(2, "0");
  return `${yyyy}-${mm}`;
};

const formatNumber = (value: number) =>
  value.toLocaleString("vi-VN", { maximumFractionDigits: 2 });

const formatDateTime = (iso: string) => {
  if (!iso) return "-";
  const dt = new Date(iso);
  if (Number.isNaN(dt.getTime())) return iso;
  return dt.toLocaleString("vi-VN", {
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit",
  });
};

function formatMinutesSeconds(seconds: number): string {
  const m = Math.floor(seconds / 60);
  const s = Math.round(seconds % 60);
  return m > 0
    ? `${m}:${String(s).padStart(2, "0")}`
    : `0:${String(s).padStart(2, "0")}`;
}

export default function AudioHistoryPage() {
  const { selectedRestaurant } = useRestaurant();
  const [billingMonth, setBillingMonth] = useState(getCurrentMonth());
  const [page, setPage] = useState(1);

  const { data, isLoading, isError } = useQuery({
    queryKey: ["saler", "translation-usage", billingMonth, page],
    queryFn: () =>
      getMyTranslationUsageApi({
        billingMonth,
        page,
        pageSize: PAGE_SIZE,
      }),
    placeholderData: (previous) => previous,
  });

  const { data: restaurantKpis } = useQuery({
    queryKey: [
      "saler",
      "analytics",
      "restaurant-kpis",
      selectedRestaurant?.restaurant_id,
    ],
    queryFn: () =>
      getRestaurantKpisApi(selectedRestaurant?.restaurant_id ?? ""),
    enabled: !!selectedRestaurant?.restaurant_id,
    staleTime: 30_000,
  });

  const totalPages = useMemo(
    () => Math.max(1, Math.ceil((data?.total_count ?? 0) / PAGE_SIZE)),
    [data?.total_count],
  );

  const avgTime = restaurantKpis?.average_listening_time_seconds ?? 0;
  const formattedAvgTime = avgTime > 0 ? formatMinutesSeconds(avgTime) : "0.0";

  return (
    <div className="max-w-7xl mx-auto animate-fade-in space-y-6">
      <div className="page-header">
        <h1 className="page-title">Lịch sử thuyết minh</h1>
        <p className="page-description">
          Theo dõi lịch sử đơn vị tính phí khi tạo thuyết minh âm thanh.
        </p>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        <div className="dashboard-card">
          <span className="stat-label">Tổng lượt nghe</span>
          <div className="mt-2 flex items-baseline gap-1">
            <span className="stat-value mono">
              {(restaurantKpis?.total_poi_plays ?? 0).toLocaleString("vi-VN")}
            </span>
            <span className="text-xs text-muted-foreground mb-0.5">lượt</span>
          </div>
        </div>

        <div className="dashboard-card">
          <span className="stat-label">Thời gian trung bình nghe 1 POI</span>
          <div className="mt-2 flex items-baseline gap-1">
            <span className="stat-value mono">{formattedAvgTime}</span>
            {avgTime > 0 && (
              <span className="text-xs text-muted-foreground mb-0.5">phút</span>
            )}
          </div>
        </div>
      </div>

      <div className="dashboard-card space-y-4">
        <div className="flex items-center justify-between gap-3">
          <div className="flex items-center gap-2">
            <History className="w-4 h-4 text-primary" />
            <h2 className="font-semibold text-base">Lịch sử đơn vị tính phí</h2>
          </div>
          <span className="text-xs text-muted-foreground mono">
            {data?.total_count ?? 0} sự kiện
          </span>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
          <div>
            <label className="text-xs font-medium text-muted-foreground">
              Tháng
            </label>
            <Input
              type="month"
              value={billingMonth}
              onChange={(e) => {
                setBillingMonth(e.target.value);
                setPage(1);
              }}
              className="mt-1"
            />
          </div>
          <div>
            <label className="text-xs font-medium text-muted-foreground">
              Tổng đơn vị tính phí
            </label>
            <div className="mt-1 h-10 rounded-md border border-input bg-muted/40 px-3 flex items-center text-sm font-medium">
              {formatNumber(data?.summary.total_billable_units ?? 0)}
            </div>
          </div>
        </div>

        <div className="overflow-x-auto rounded-lg border border-border/70 bg-card">
          <table className="data-table">
            <thead>
              <tr>
                <th>Thời gian</th>
                <th>Người bán</th>
                <th>ID người bán</th>
                <th>Hành động</th>
                <th>Ký tự đầu vào</th>
                <th>Đơn vị tính phí</th>
              </tr>
            </thead>
            <tbody>
              {isLoading && (
                <tr>
                  <td
                    colSpan={6}
                    className="text-center py-8 text-muted-foreground"
                  >
                    Đang tải lịch sử sử dụng...
                  </td>
                </tr>
              )}
              {isError && (
                <tr>
                  <td colSpan={6} className="text-center py-8 text-destructive">
                    Không thể tải lịch sử đơn vị tính phí.
                  </td>
                </tr>
              )}
              {!isLoading && !isError && (data?.items.length ?? 0) === 0 && (
                <tr>
                  <td
                    colSpan={6}
                    className="text-center py-8 text-muted-foreground"
                  >
                    Chưa có lịch sử đơn vị tính phí theo bộ lọc.
                  </td>
                </tr>
              )}
              {!isLoading &&
                !isError &&
                (data?.items ?? []).map((item) => (
                  <tr key={item.usage_event_id}>
                    <td className="mono text-xs whitespace-nowrap">
                      {formatDateTime(item.created_at_utc)}
                    </td>
                    <td className="font-medium">
                      {item.seller_username || "(không rõ)"}
                    </td>
                    <td className="mono text-xs text-muted-foreground">
                      {item.seller_user_id}
                    </td>
                    <td className="mono text-xs">{item.action_type}</td>
                    <td className="mono text-xs">
                      {formatNumber(item.input_chars)}
                    </td>
                    <td className="mono text-xs">
                      {formatNumber(item.billable_units)}
                    </td>
                  </tr>
                ))}
            </tbody>
          </table>
        </div>

        <div className="mt-1 flex items-center justify-end gap-2">
          <button
            type="button"
            className="px-3 py-1.5 rounded-md border text-xs font-medium disabled:opacity-50"
            disabled={page <= 1}
            onClick={() => setPage((p) => Math.max(1, p - 1))}
          >
            Trang trước
          </button>
          <span className="text-xs text-muted-foreground mono">
            {page}/{totalPages}
          </span>
          <button
            type="button"
            className="px-3 py-1.5 rounded-md border text-xs font-medium disabled:opacity-50"
            disabled={page >= totalPages}
            onClick={() => setPage((p) => p + 1)}
          >
            Trang sau
          </button>
        </div>
      </div>
    </div>
  );
}
