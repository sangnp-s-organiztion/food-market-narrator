import { useQuery } from "@tanstack/react-query";
import { useMemo, useState } from "react";
import AdminLayout from "@/components/AdminLayout";
import { Input } from "@/components/ui/input";
import {
  translationBillingApi,
  type AudioUsageLedgerItem,
  type TranslationMonthlyBillingItem,
  type TranslationUsageLedgerItem,
} from "@/lib/adminApi";

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

const formatActionType = (actionType: string) => {
  switch ((actionType ?? "").toLowerCase()) {
    case "translate":
      return "dịch";
    case "create_audio":
      return "tạo tệp thuyết minh";
    default:
      return actionType;
  }
};

const MonthlyBillingTable = ({
  items,
}: {
  items: TranslationMonthlyBillingItem[];
}) => {
  if (items.length === 0) {
    return (
      <tr>
        <td colSpan={6} className="text-center py-8 text-muted-foreground">
          Chưa có dữ liệu chi phí theo bộ lọc.
        </td>
      </tr>
    );
  }

  return (
    <>
      {items.map((item) => (
        <tr key={`${item.sellerUserId}-${item.billingMonth}`}>
          <td className="font-medium">{item.sellerUsername || "(không rõ)"}</td>
          <td className="mono text-xs">{item.billingMonth}</td>
          <td className="mono text-xs">{formatNumber(item.totalRequests)}</td>
          <td className="mono text-xs text-emerald-700">
            {formatNumber(item.successRequests)}
          </td>
          <td className="mono text-xs text-red-600">
            {formatNumber(item.failedRequests)}
          </td>
          <td className="mono text-xs">
            {formatNumber(item.totalBillableUnits)}
          </td>
        </tr>
      ))}
    </>
  );
};

const UsageLedgerTable = ({
  items,
}: {
  items: TranslationUsageLedgerItem[];
}) => {
  if (items.length === 0) {
    return (
      <tr>
        <td colSpan={5} className="text-center py-8 text-muted-foreground">
          Chưa có lịch sử đơn vị tính phí theo bộ lọc.
        </td>
      </tr>
    );
  }

  return (
    <>
      {items.map((item) => (
        <tr key={item.usageEventId}>
          <td className="mono text-xs whitespace-nowrap">
            {formatDateTime(item.createdAtUtc)}
          </td>
          <td className="font-medium">{item.sellerUsername || "(không rõ)"}</td>
          <td className="mono text-xs">{formatActionType(item.actionType)}</td>
          <td className="mono text-xs">{formatNumber(item.inputChars)}</td>
          <td className="mono text-xs">{formatNumber(item.billableUnits)}</td>
        </tr>
      ))}
    </>
  );
};

const AudioUsageLedgerTable = ({
  items,
}: {
  items: AudioUsageLedgerItem[];
}) => {
  if (items.length === 0) {
    return (
      <tr>
        <td colSpan={7} className="text-center py-8 text-muted-foreground">
          Chưa có lịch sử tạo audio theo bộ lọc.
        </td>
      </tr>
    );
  }

  return (
    <>
      {items.map((item) => (
        <tr key={item.usageEventId}>
          <td className="mono text-xs whitespace-nowrap">
            {formatDateTime(item.createdAtUtc)}
          </td>
          <td className="font-medium">{item.sellerUsername || "(không rõ)"}</td>
          <td className="mono text-xs">{item.restaurantId}</td>
          <td className="mono text-xs">{item.audioId ?? "-"}</td>
          <td className="mono text-xs">{formatActionType(item.actionType)}</td>
          <td className="mono text-xs">{formatNumber(item.inputChars)}</td>
          <td className="mono text-xs">{formatNumber(item.billableUnits)}</td>
        </tr>
      ))}
    </>
  );
};

const TranslationBillingPage = () => {
  const [billingMonth, setBillingMonth] = useState(getCurrentMonth());
  const [sellerUserIdRaw, setSellerUserIdRaw] = useState("");
  const [monthlyPage, setMonthlyPage] = useState(1);
  const [usagePage, setUsagePage] = useState(1);
  const [audioUsagePage, setAudioUsagePage] = useState(1);

  const sellerUserId = useMemo(() => {
    if (!sellerUserIdRaw.trim()) return undefined;
    const parsed = Number(sellerUserIdRaw.trim());
    return Number.isFinite(parsed) && parsed > 0 ? parsed : undefined;
  }, [sellerUserIdRaw]);

  const {
    data: monthlyData,
    isLoading: monthlyLoading,
    isError: monthlyError,
  } = useQuery({
    queryKey: [
      "admin",
      "translation-billing",
      "monthly",
      billingMonth,
      sellerUserId,
      monthlyPage,
    ],
    queryFn: () =>
      translationBillingApi.getMonthly({
        billingMonth,
        sellerUserId,
        page: monthlyPage,
        pageSize: PAGE_SIZE,
      }),
    placeholderData: (previous) => previous,
  });

  const {
    data: usageData,
    isLoading: usageLoading,
    isError: usageError,
  } = useQuery({
    queryKey: [
      "admin",
      "translation-billing",
      "usage",
      billingMonth,
      sellerUserId,
      usagePage,
    ],
    queryFn: () =>
      translationBillingApi.getUsage({
        billingMonth,
        sellerUserId,
        page: usagePage,
        pageSize: PAGE_SIZE,
      }),
    placeholderData: (previous) => previous,
  });

  const {
    data: audioUsageData,
    isLoading: audioUsageLoading,
    isError: audioUsageError,
  } = useQuery({
    queryKey: [
      "admin",
      "translation-billing",
      "audio-usage",
      billingMonth,
      sellerUserId,
      audioUsagePage,
    ],
    queryFn: () =>
      translationBillingApi.getAudioUsage({
        billingMonth,
        sellerUserId,
        page: audioUsagePage,
        pageSize: PAGE_SIZE,
      }),
    placeholderData: (previous) => previous,
  });

  const monthlyTotalPages = Math.max(
    1,
    Math.ceil((monthlyData?.totalCount ?? 0) / PAGE_SIZE),
  );

  const usageTotalPages = Math.max(
    1,
    Math.ceil((usageData?.totalCount ?? 0) / PAGE_SIZE),
  );

  const audioUsageTotalPages = Math.max(
    1,
    Math.ceil((audioUsageData?.totalCount ?? 0) / PAGE_SIZE),
  );

  return (
    <AdminLayout>
      <div className="page-header">
        <div>
          <h1 className="page-title">Dịch vụ</h1>
          <p className="text-sm text-muted-foreground mt-0.5">
            Theo dõi lịch sử và đơn vị tính phí theo tháng
          </p>
        </div>
      </div>

      <div className="max-w-7xl mx-auto px-8 py-6 space-y-6">
        <div className="stat-card">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div>
              <label className="stat-label">Tháng</label>
              <Input
                type="month"
                value={billingMonth}
                onChange={(e) => {
                  setBillingMonth(e.target.value);
                  setMonthlyPage(1);
                  setUsagePage(1);
                  setAudioUsagePage(1);
                }}
                className="mt-1"
              />
            </div>
            <div>
              <label className="stat-label">ID người bán</label>
              <Input
                value={sellerUserIdRaw}
                onChange={(e) => {
                  setSellerUserIdRaw(e.target.value);
                  setMonthlyPage(1);
                  setUsagePage(1);
                  setAudioUsagePage(1);
                }}
                className="mt-1"
                placeholder="Để trống = tất cả"
              />
            </div>
          </div>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div className="stat-card">
            <span className="stat-label">Tổng yêu cầu</span>
            <div className="stat-value mono mt-2">
              {formatNumber(monthlyData?.summary.totalRequests ?? 0)}
            </div>
          </div>
          <div className="stat-card">
            <span className="stat-label">Tổng đơn vị tính phí</span>
            <div className="stat-value mono mt-2">
              {formatNumber(monthlyData?.summary.totalBillableUnits ?? 0)}
            </div>
          </div>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div className="stat-card">
            <span className="stat-label">Tổng sự kiện tạo audio</span>
            <div className="stat-value mono mt-2">
              {formatNumber(audioUsageData?.summary.eventCount ?? 0)}
            </div>
          </div>
          <div className="stat-card">
            <span className="stat-label">Tổng đơn vị tính phí audio</span>
            <div className="stat-value mono mt-2">
              {formatNumber(audioUsageData?.summary.totalBillableUnits ?? 0)}
            </div>
          </div>
        </div>

        <div className="stat-card">
          <div className="flex items-center justify-between mb-3">
            <h2 className="text-lg font-semibold">
              Tổng hợp theo người bán/tháng
            </h2>
            <span className="text-xs text-muted-foreground mono">
              {monthlyData?.totalCount ?? 0} bản ghi
            </span>
          </div>

          <table className="data-table">
            <thead>
              <tr>
                <th>Người bán</th>
                <th>Tháng</th>
                <th>Yêu cầu</th>
                <th>Thành công</th>
                <th>Thất bại</th>
                <th>Đơn vị tính phí</th>
              </tr>
            </thead>
            <tbody>
              {monthlyLoading && (
                <tr>
                  <td
                    colSpan={6}
                    className="text-center py-8 text-muted-foreground"
                  >
                    Đang tải dữ liệu chi phí...
                  </td>
                </tr>
              )}
              {monthlyError && (
                <tr>
                  <td colSpan={6} className="text-center py-8 text-destructive">
                    Không thể tải dữ liệu chi phí theo tháng.
                  </td>
                </tr>
              )}
              {!monthlyLoading && !monthlyError && (
                <MonthlyBillingTable items={monthlyData?.items ?? []} />
              )}
            </tbody>
          </table>

          <div className="mt-3 flex items-center justify-end gap-2">
            <button
              type="button"
              className="px-3 py-1.5 rounded-md border text-xs font-medium disabled:opacity-50"
              disabled={monthlyPage <= 1}
              onClick={() => setMonthlyPage((p) => Math.max(1, p - 1))}
            >
              Trang trước
            </button>
            <span className="text-xs text-muted-foreground mono">
              {monthlyPage}/{monthlyTotalPages}
            </span>
            <button
              type="button"
              className="px-3 py-1.5 rounded-md border text-xs font-medium disabled:opacity-50"
              disabled={monthlyPage >= monthlyTotalPages}
              onClick={() => setMonthlyPage((p) => p + 1)}
            >
              Trang sau
            </button>
          </div>
        </div>

        <div className="stat-card">
          <div className="flex items-center justify-between mb-3">
            <h2 className="text-lg font-semibold">
              Lịch sử sử dụng dịch vụ dịch thuật
            </h2>
            <span className="text-xs text-muted-foreground mono">
              {usageData?.totalCount ?? 0} sự kiện
            </span>
          </div>

          <table className="data-table">
            <thead>
              <tr>
                <th>Thời gian</th>
                <th>Người bán</th>
                <th>Hành động</th>
                <th>Ký tự đầu vào</th>
                <th>Đơn vị tính phí</th>
              </tr>
            </thead>
            <tbody>
              {usageLoading && (
                <tr>
                  <td
                    colSpan={6}
                    className="text-center py-8 text-muted-foreground"
                  >
                    Đang tải lịch sử sử dụng...
                  </td>
                </tr>
              )}
              {usageError && (
                <tr>
                  <td colSpan={6} className="text-center py-8 text-destructive">
                    Không thể tải lịch sử đơn vị tính phí.
                  </td>
                </tr>
              )}
              {!usageLoading && !usageError && (
                <UsageLedgerTable items={usageData?.items ?? []} />
              )}
            </tbody>
          </table>

          <div className="mt-3 flex items-center justify-end gap-2">
            <button
              type="button"
              className="px-3 py-1.5 rounded-md border text-xs font-medium disabled:opacity-50"
              disabled={usagePage <= 1}
              onClick={() => setUsagePage((p) => Math.max(1, p - 1))}
            >
              Trang trước
            </button>
            <span className="text-xs text-muted-foreground mono">
              {usagePage}/{usageTotalPages}
            </span>
            <button
              type="button"
              className="px-3 py-1.5 rounded-md border text-xs font-medium disabled:opacity-50"
              disabled={usagePage >= usageTotalPages}
              onClick={() => setUsagePage((p) => p + 1)}
            >
              Trang sau
            </button>
          </div>
        </div>

        <div className="stat-card">
          <div className="flex items-center justify-between mb-3">
            <h2 className="text-lg font-semibold">
              Lịch sử tạo tệp thuyết minh
            </h2>
            <span className="text-xs text-muted-foreground mono">
              {audioUsageData?.totalCount ?? 0} sự kiện
            </span>
          </div>

          <table className="data-table">
            <thead>
              <tr>
                <th>Thời gian</th>
                <th>Người bán</th>
                <th>Nhà hàng</th>
                <th>Audio</th>
                <th>Hành động</th>
                <th>Ký tự đầu vào</th>
                <th>Đơn vị tính phí</th>
              </tr>
            </thead>
            <tbody>
              {audioUsageLoading && (
                <tr>
                  <td
                    colSpan={7}
                    className="text-center py-8 text-muted-foreground"
                  >
                    Đang tải lịch sử tạo audio...
                  </td>
                </tr>
              )}
              {audioUsageError && (
                <tr>
                  <td colSpan={7} className="text-center py-8 text-destructive">
                    Không thể tải lịch sử tạo audio.
                  </td>
                </tr>
              )}
              {!audioUsageLoading && !audioUsageError && (
                <AudioUsageLedgerTable items={audioUsageData?.items ?? []} />
              )}
            </tbody>
          </table>

          <div className="mt-3 flex items-center justify-end gap-2">
            <button
              type="button"
              className="px-3 py-1.5 rounded-md border text-xs font-medium disabled:opacity-50"
              disabled={audioUsagePage <= 1}
              onClick={() => setAudioUsagePage((p) => Math.max(1, p - 1))}
            >
              Trang trước
            </button>
            <span className="text-xs text-muted-foreground mono">
              {audioUsagePage}/{audioUsageTotalPages}
            </span>
            <button
              type="button"
              className="px-3 py-1.5 rounded-md border text-xs font-medium disabled:opacity-50"
              disabled={audioUsagePage >= audioUsageTotalPages}
              onClick={() => setAudioUsagePage((p) => p + 1)}
            >
              Trang sau
            </button>
          </div>
        </div>
      </div>
    </AdminLayout>
  );
};

export default TranslationBillingPage;
