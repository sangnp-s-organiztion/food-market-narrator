import { useMemo, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { History } from "lucide-react";
import { Input } from "@/components/ui/input";
import {
  getLanguagesApi,
  getMyAudioUsageApi,
  getMyTranslationUsageApi,
  getRestaurantAudiosApi,
  getRestaurantKpisApi,
} from "@/services/api";
import { useRestaurant } from "@/contexts/RestaurantContext";

const PAGE_SIZE = 20;
const USAGE_MULTIPLIER = 1.2;

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
      return "Dịch";
    case "create_audio":
      return "Tạo tệp thuyết minh";
    default:
      return actionType;
  }
};

const calculateUsageLevel = (inputChars: number) =>
  inputChars * USAGE_MULTIPLIER;

type AudioMeta = {
  language_id: number;
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
  const [translationPage, setTranslationPage] = useState(1);
  const [audioPage, setAudioPage] = useState(1);

  const {
    data: translationData,
    isLoading: isTranslationLoading,
    isError: isTranslationError,
  } = useQuery({
    queryKey: ["saler", "translation-usage", billingMonth, translationPage],
    queryFn: () =>
      getMyTranslationUsageApi({
        billingMonth,
        page: translationPage,
        pageSize: PAGE_SIZE,
      }),
    placeholderData: (previous) => previous,
  });

  const {
    data: audioData,
    isLoading: isAudioLoading,
    isError: isAudioError,
  } = useQuery({
    queryKey: ["saler", "audio-usage", billingMonth, audioPage],
    queryFn: () =>
      getMyAudioUsageApi({
        billingMonth,
        page: audioPage,
        pageSize: PAGE_SIZE,
      }),
    placeholderData: (previous) => previous,
  });

  const audioHistoryRestaurantIds = useMemo(
    () =>
      Array.from(
        new Set(
          (audioData?.items ?? [])
            .map((item) => item.restaurant_id)
            .filter((id): id is string => Boolean(id)),
        ),
      ),
    [audioData?.items],
  );

  const audioHistoryAudioIds = useMemo(
    () =>
      Array.from(
        new Set(
          (audioData?.items ?? [])
            .map((item) => item.audio_id)
            .filter((id): id is number => typeof id === "number" && id > 0),
        ),
      ).sort((a, b) => a - b),
    [audioData?.items],
  );

  const { data: languages } = useQuery({
    queryKey: ["saler", "languages", "audio-history"],
    queryFn: () => getLanguagesApi(),
    staleTime: 5 * 60_000,
  });

  const { data: audioMetaById } = useQuery({
    queryKey: [
      "saler",
      "audio-meta",
      "history",
      ...audioHistoryRestaurantIds,
      "audio-ids",
      ...audioHistoryAudioIds,
    ],
    queryFn: async () => {
      const lists = await Promise.all(
        audioHistoryRestaurantIds.map((restaurantId) =>
          getRestaurantAudiosApi(restaurantId),
        ),
      );

      const metadata: Record<number, AudioMeta> = {};
      lists.forEach((audios) => {
        audios.forEach((audio) => {
          metadata[audio.audio_id] = {
            language_id: audio.language_id,
          };
        });
      });

      return metadata;
    },
    enabled: audioHistoryRestaurantIds.length > 0,
    staleTime: 15_000,
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

  const translationTotalPages = useMemo(
    () =>
      Math.max(1, Math.ceil((translationData?.total_count ?? 0) / PAGE_SIZE)),
    [translationData?.total_count],
  );

  const audioTotalPages = useMemo(
    () => Math.max(1, Math.ceil((audioData?.total_count ?? 0) / PAGE_SIZE)),
    [audioData?.total_count],
  );

  const avgTime = restaurantKpis?.average_listening_time_seconds ?? 0;
  const formattedAvgTime = avgTime > 0 ? formatMinutesSeconds(avgTime) : "0.0";
  const translationSummaryUsage = calculateUsageLevel(
    translationData?.summary.total_billable_units ?? 0,
  );
  const audioSummaryUsage = calculateUsageLevel(
    audioData?.summary.total_billable_units ?? 0,
  );

  const languageNameById = useMemo(() => {
    const map: Record<number, string> = {};
    (languages ?? []).forEach((language) => {
      map[language.language_id] = language.name || language.code || "-";
    });
    return map;
  }, [languages]);

  const getAudioLanguageLabel = (audioId: number | null) => {
    if (!audioId) return "-";
    const meta = audioMetaById?.[audioId];
    if (!meta) return "-";
    return languageNameById[meta.language_id] ?? "-";
  };

  return (
    <div className="max-w-7xl mx-auto animate-fade-in space-y-6">
      <div className="page-header">
        <h1 className="page-title">Lịch sử thuyết minh</h1>
        <p className="page-description">
          Theo dõi mức sử dụng dịch vụ dịch thuật và tạo tệp thuyết minh.
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
          <span className="stat-label">
            Thời gian trung bình nghe thuyết minh
          </span>
          <div className="mt-2 flex items-baseline gap-1">
            <span className="stat-value mono">{formattedAvgTime}</span>
            {avgTime > 0 && (
              <span className="text-xs text-muted-foreground mb-0.5">phút</span>
            )}
          </div>
        </div>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        <div className="dashboard-card">
          <span className="stat-label">
            Tổng mức sử dụng dịch vụ dịch thuật
          </span>
          <div className="mt-2 flex items-baseline gap-1">
            <span className="stat-value mono">
              {formatNumber(translationSummaryUsage)}
            </span>
          </div>
        </div>

        <div className="dashboard-card">
          <span className="stat-label">
            Tổng mức sử dụng dịch vụ tạo tệp thuyết minh
          </span>
          <div className="mt-2 flex items-baseline gap-1">
            <span className="stat-value mono">
              {formatNumber(audioSummaryUsage)}
            </span>
          </div>
        </div>
      </div>

      <div className="dashboard-card space-y-4">
        <div className="flex items-center justify-between gap-3">
          <div className="flex items-center gap-2">
            <History className="w-4 h-4 text-primary" />
            <h2 className="font-semibold text-base">
              Lịch sử sử dụng dịch vụ dịch thuật
            </h2>
          </div>
          <span className="text-xs text-muted-foreground mono">
            {translationData?.total_count ?? 0} sự kiện
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
                setTranslationPage(1);
                setAudioPage(1);
              }}
              className="mt-1"
            />
          </div>
          <div>
            <label className="text-xs font-medium text-muted-foreground">
              Tổng mức sử dụng dịch vụ dịch thuật
            </label>
            <div className="mt-1 h-10 rounded-md border border-input bg-muted/40 px-3 flex items-center text-sm font-medium">
              {formatNumber(translationSummaryUsage)}
            </div>
          </div>
        </div>

        <div className="overflow-x-auto rounded-lg border border-border/70 bg-card">
          <table className="data-table">
            <thead>
              <tr>
                <th>Thời gian</th>
                <th>Hành động</th>
                <th>Ký tự đầu vào</th>
                <th>Mức sử dụng</th>
              </tr>
            </thead>
            <tbody>
              {isTranslationLoading && (
                <tr>
                  <td
                    colSpan={4}
                    className="text-center py-8 text-muted-foreground"
                  >
                    Đang tải lịch sử sử dụng...
                  </td>
                </tr>
              )}
              {isTranslationError && (
                <tr>
                  <td colSpan={4} className="text-center py-8 text-destructive">
                    Không thể tải lịch sử sử dụng dịch vụ.
                  </td>
                </tr>
              )}
              {!isTranslationLoading &&
                !isTranslationError &&
                (translationData?.items.length ?? 0) === 0 && (
                  <tr>
                    <td
                      colSpan={4}
                      className="text-center py-8 text-muted-foreground"
                    >
                      Chưa có lịch sử sử dụng dịch vụ dịch thuật theo bộ lọc.
                    </td>
                  </tr>
                )}
              {!isTranslationLoading &&
                !isTranslationError &&
                (translationData?.items ?? []).map((item) => (
                  <tr key={item.usage_event_id}>
                    <td className="mono text-xs whitespace-nowrap">
                      {formatDateTime(item.created_at_utc)}
                    </td>
                    <td className="mono text-xs">
                      {formatActionType(item.action_type)}
                    </td>
                    <td className="mono text-xs">
                      {formatNumber(item.input_chars)}
                    </td>
                    <td className="mono text-xs">
                      {formatNumber(calculateUsageLevel(item.input_chars))}
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
            disabled={translationPage <= 1}
            onClick={() => setTranslationPage((p) => Math.max(1, p - 1))}
          >
            Trang trước
          </button>
          <span className="text-xs text-muted-foreground mono">
            {translationPage}/{translationTotalPages}
          </span>
          <button
            type="button"
            className="px-3 py-1.5 rounded-md border text-xs font-medium disabled:opacity-50"
            disabled={translationPage >= translationTotalPages}
            onClick={() => setTranslationPage((p) => p + 1)}
          >
            Trang sau
          </button>
        </div>
      </div>

      <div className="dashboard-card space-y-4">
        <div className="flex items-center justify-between gap-3">
          <div className="flex items-center gap-2">
            <History className="w-4 h-4 text-primary" />
            <h2 className="font-semibold text-base">
              Lịch sử tạo tệp thuyết minh
            </h2>
          </div>
          <span className="text-xs text-muted-foreground mono">
            {audioData?.total_count ?? 0} sự kiện
          </span>
        </div>

        <div>
          <label className="text-xs font-medium text-muted-foreground">
            Tổng mức sử dụng dịch vụ tạo tệp thuyết minh
          </label>
          <div className="mt-1 h-10 rounded-md border border-input bg-muted/40 px-3 flex items-center text-sm font-medium">
            {formatNumber(audioSummaryUsage)}
          </div>
        </div>

        <div className="overflow-x-auto rounded-lg border border-border/70 bg-card">
          <table className="data-table">
            <thead>
              <tr>
                <th>Thời gian</th>
                <th>Nhà hàng</th>
                <th>Ngôn ngữ</th>
                <th>Hành động</th>
                <th>Ký tự đầu vào</th>
                <th>Mức sử dụng</th>
              </tr>
            </thead>
            <tbody>
              {isAudioLoading && (
                <tr>
                  <td
                    colSpan={6}
                    className="text-center py-8 text-muted-foreground"
                  >
                    Đang tải lịch sử tạo audio...
                  </td>
                </tr>
              )}
              {isAudioError && (
                <tr>
                  <td colSpan={6} className="text-center py-8 text-destructive">
                    Không thể tải lịch sử tạo audio.
                  </td>
                </tr>
              )}
              {!isAudioLoading &&
                !isAudioError &&
                (audioData?.items.length ?? 0) === 0 && (
                  <tr>
                    <td
                      colSpan={6}
                      className="text-center py-8 text-muted-foreground"
                    >
                      Chưa có lịch sử tạo audio theo bộ lọc.
                    </td>
                  </tr>
                )}
              {!isAudioLoading &&
                !isAudioError &&
                (audioData?.items ?? []).map((item) => (
                  <tr key={item.usage_event_id}>
                    <td className="mono text-xs whitespace-nowrap">
                      {formatDateTime(item.created_at_utc)}
                    </td>
                    <td className="mono text-xs">{item.restaurant_id}</td>
                    <td className="mono text-xs">
                      {getAudioLanguageLabel(item.audio_id)}
                    </td>
                    <td className="mono text-xs">
                      {formatActionType(item.action_type)}
                    </td>
                    <td className="mono text-xs">
                      {formatNumber(item.input_chars)}
                    </td>
                    <td className="mono text-xs">
                      {formatNumber(calculateUsageLevel(item.input_chars))}
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
            disabled={audioPage <= 1}
            onClick={() => setAudioPage((p) => Math.max(1, p - 1))}
          >
            Trang trước
          </button>
          <span className="text-xs text-muted-foreground mono">
            {audioPage}/{audioTotalPages}
          </span>
          <button
            type="button"
            className="px-3 py-1.5 rounded-md border text-xs font-medium disabled:opacity-50"
            disabled={audioPage >= audioTotalPages}
            onClick={() => setAudioPage((p) => p + 1)}
          >
            Trang sau
          </button>
        </div>
      </div>
    </div>
  );
}
