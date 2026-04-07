import { useQuery } from "@tanstack/react-query";
import { useMemo, useState } from "react";
import AdminLayout from "@/components/AdminLayout";
import { Input } from "@/components/ui/input";
import {
  translationBillingApi,
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

const MonthlyBillingTable = ({
  items,
}: {
  items: TranslationMonthlyBillingItem[];
}) => {
  if (items.length === 0) {
    return (
      <tr>
        <td colSpan={8} className="text-center py-8 text-muted-foreground">
          Chưa có dữ liệu billing theo bộ lọc.
        </td>
      </tr>
    );
  }

  return (
    <>
      {items.map((item) => (
        <tr key={`${item.sellerUserId}-${item.billingMonth}`}>
          <td className="font-medium">{item.sellerUsername || "(không rõ)"}</td>
          <td className="mono text-xs text-muted-foreground">
            {item.sellerUserId}
          </td>
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
          <td className="mono text-xs font-medium">
            {formatNumber(item.totalAmount)} {item.currency}
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
        <td colSpan={9} className="text-center py-8 text-muted-foreground">
          Chưa có lịch sử sử dụng token theo bộ lọc.
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
          <td className="mono text-xs text-muted-foreground">
            {item.sellerUserId}
          </td>
          <td className="mono text-xs">{item.actionType}</td>
          <td className="mono text-xs">{item.status}</td>
          <td className="mono text-xs">{formatNumber(item.inputChars)}</td>
          <td className="mono text-xs">{formatNumber(item.billableUnits)}</td>
          <td className="mono text-xs font-medium">
            {formatNumber(item.totalAmount)} {item.currency}
          </td>
          <td className="mono text-xs text-muted-foreground">
            {item.provider}
          </td>
        </tr>
      ))}
    </>
  );
};

const TranslationBillingPage = () => {
  const [billingMonth, setBillingMonth] = useState(getCurrentMonth());
  const [sellerUserIdRaw, setSellerUserIdRaw] = useState("");
  const [usageStatus, setUsageStatus] = useState<"all" | "billable" | "failed">(
    "all",
  );
  const [monthlyPage, setMonthlyPage] = useState(1);
  const [usagePage, setUsagePage] = useState(1);

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
      usageStatus,
      usagePage,
    ],
    queryFn: () =>
      translationBillingApi.getUsage({
        billingMonth,
        sellerUserId,
        status: usageStatus === "all" ? undefined : usageStatus,
        page: usagePage,
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

  return (
    <AdminLayout>
      <div className="page-header">
        <div>
          <h1 className="page-title">Billing token dịch</h1>
          <p className="text-sm text-muted-foreground mt-0.5">
            Theo dõi lịch sử usage token dịch và tổng tiền theo tháng
          </p>
        </div>
      </div>

      <div className="max-w-7xl mx-auto px-8 py-6 space-y-6">
        <div className="stat-card">
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <div>
              <label className="stat-label">Tháng billing</label>
              <Input
                type="month"
                value={billingMonth}
                onChange={(e) => {
                  setBillingMonth(e.target.value);
                  setMonthlyPage(1);
                  setUsagePage(1);
                }}
                className="mt-1"
              />
            </div>
            <div>
              <label className="stat-label">Seller User ID</label>
              <Input
                value={sellerUserIdRaw}
                onChange={(e) => {
                  setSellerUserIdRaw(e.target.value);
                  setMonthlyPage(1);
                  setUsagePage(1);
                }}
                className="mt-1"
                placeholder="Để trống = tất cả"
              />
            </div>
            <div>
              <label className="stat-label">Trạng thái usage</label>
              <select
                className="mt-1 h-10 w-full rounded-md border border-input bg-background px-3 text-sm"
                value={usageStatus}
                onChange={(e) => {
                  setUsageStatus(
                    e.target.value as "all" | "billable" | "failed",
                  );
                  setUsagePage(1);
                }}
              >
                <option value="all">Tất cả</option>
                <option value="billable">Billable</option>
                <option value="failed">Failed</option>
              </select>
            </div>
          </div>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
          <div className="stat-card">
            <span className="stat-label">Tổng requests</span>
            <div className="stat-value mono mt-2">
              {formatNumber(monthlyData?.summary.totalRequests ?? 0)}
            </div>
          </div>
          <div className="stat-card">
            <span className="stat-label">Tổng billable units</span>
            <div className="stat-value mono mt-2">
              {formatNumber(monthlyData?.summary.totalBillableUnits ?? 0)}
            </div>
          </div>
          <div className="stat-card">
            <span className="stat-label">Tổng tiền</span>
            <div className="stat-value mono mt-2">
              {formatNumber(monthlyData?.summary.totalAmount ?? 0)}{" "}
              {monthlyData?.summary.currency ?? "USD"}
            </div>
          </div>
        </div>

        <div className="stat-card">
          <div className="flex items-center justify-between mb-3">
            <h2 className="text-lg font-semibold">
              Tổng hợp theo seller/tháng
            </h2>
            <span className="text-xs text-muted-foreground mono">
              {monthlyData?.totalCount ?? 0} bản ghi
            </span>
          </div>

          <table className="data-table">
            <thead>
              <tr>
                <th>Seller</th>
                <th>Seller ID</th>
                <th>Tháng</th>
                <th>Requests</th>
                <th>Success</th>
                <th>Failed</th>
                <th>Billable Units</th>
                <th>Tổng tiền</th>
              </tr>
            </thead>
            <tbody>
              {monthlyLoading && (
                <tr>
                  <td
                    colSpan={8}
                    className="text-center py-8 text-muted-foreground"
                  >
                    Đang tải dữ liệu billing...
                  </td>
                </tr>
              )}
              {monthlyError && (
                <tr>
                  <td colSpan={8} className="text-center py-8 text-destructive">
                    Không thể tải dữ liệu billing theo tháng.
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
            <h2 className="text-lg font-semibold">Lịch sử usage token dịch</h2>
            <span className="text-xs text-muted-foreground mono">
              {usageData?.totalCount ?? 0} sự kiện
            </span>
          </div>

          <table className="data-table">
            <thead>
              <tr>
                <th>Thời gian</th>
                <th>Seller</th>
                <th>Seller ID</th>
                <th>Action</th>
                <th>Status</th>
                <th>Input chars</th>
                <th>Billable units</th>
                <th>Tổng tiền</th>
                <th>Provider</th>
              </tr>
            </thead>
            <tbody>
              {usageLoading && (
                <tr>
                  <td
                    colSpan={9}
                    className="text-center py-8 text-muted-foreground"
                  >
                    Đang tải lịch sử usage...
                  </td>
                </tr>
              )}
              {usageError && (
                <tr>
                  <td colSpan={9} className="text-center py-8 text-destructive">
                    Không thể tải lịch sử usage token.
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
      </div>
    </AdminLayout>
  );
};

export default TranslationBillingPage;
