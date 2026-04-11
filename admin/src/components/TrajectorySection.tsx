import { useEffect, useMemo, useRef, useState } from "react";
import L from "leaflet";
import "leaflet/dist/leaflet.css";
import { CalendarDays, LocateFixed, Route } from "lucide-react";
import { Calendar } from "@/components/ui/calendar";
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from "@/components/ui/popover";
import type { MovementPath } from "@/types/analytics";
import type { DateRange } from "react-day-picker";

interface TrajectorySectionProps {
  movementPaths?: MovementPath[];
}

type DateSortOrder = "desc" | "asc";
type SortedPath = MovementPath & { latestTimestamp: number };

const MAP_CENTER: [number, number] = [10.761, 106.703];
const MAP_ZOOM = 16;
const PATH_COLORS = [
  "hsl(221, 83%, 53%)",
  "hsl(199, 89%, 48%)",
  "hsl(142, 71%, 45%)",
  "hsl(280, 67%, 54%)",
  "hsl(25, 95%, 53%)",
];
const SESSION_IDS_PER_PAGE = 20;
const TABLE_ROWS_PER_PAGE = 20;

const toTimestamp = (value: string): number => {
  const parsed = Date.parse(value);
  return Number.isFinite(parsed) ? parsed : 0;
};

const formatSessionDate = (timestamp: number): string => {
  if (!timestamp) return "Không rõ ngày";
  return new Date(timestamp).toLocaleString("vi-VN");
};

const isWithinRange = (
  timestamp: number,
  range: DateRange | undefined,
): boolean => {
  if (!range?.from && !range?.to) {
    return true;
  }

  const date = new Date(timestamp);

  if (range.from) {
    const from = new Date(range.from);
    from.setHours(0, 0, 0, 0);
    if (date < from) {
      return false;
    }
  }

  if (range.to) {
    const to = new Date(range.to);
    to.setHours(23, 59, 59, 999);
    if (date > to) {
      return false;
    }
  }

  return true;
};

const formatRangeLabel = (range: DateRange | undefined): string => {
  if (!range?.from && !range?.to) {
    return "Chọn khoảng thời gian";
  }

  const fromLabel = range.from ? range.from.toLocaleDateString("vi-VN") : "...";
  const toLabel = range.to ? range.to.toLocaleDateString("vi-VN") : "...";
  return `${fromLabel} - ${toLabel}`;
};

export function TrajectorySection({
  movementPaths = [],
}: TrajectorySectionProps) {
  const [baseLayer, setBaseLayer] = useState<"map" | "satellite">("map");
  const [selectedSessionId, setSelectedSessionId] = useState<string | null>(
    null,
  );
  const [dateSortOrder, setDateSortOrder] = useState<DateSortOrder>("desc");
  const [selectedRange, setSelectedRange] = useState<DateRange | undefined>();
  const [currentPage, setCurrentPage] = useState(1);
  const [tablePage, setTablePage] = useState(1);
  const mapRef = useRef<HTMLDivElement>(null);
  const mapInstanceRef = useRef<L.Map | null>(null);
  const pathLayerRef = useRef<L.LayerGroup | null>(null);
  const tileLayersRef = useRef<{
    map: L.TileLayer;
    satellite: L.TileLayer;
  } | null>(null);

  const sortedMovementPaths = useMemo<SortedPath[]>(() => {
    return [...movementPaths]
      .map((path) => {
        const sortedPoints = [...path.points].sort(
          (a, b) => toTimestamp(a.timestamp) - toTimestamp(b.timestamp),
        );

        const latestTimestamp = sortedPoints.length
          ? toTimestamp(sortedPoints[sortedPoints.length - 1].timestamp)
          : 0;

        return {
          ...path,
          points: sortedPoints,
          latestTimestamp,
        };
      })
      .sort((a, b) => {
        if (dateSortOrder === "asc") {
          return a.latestTimestamp - b.latestTimestamp;
        }
        return b.latestTimestamp - a.latestTimestamp;
      });
  }, [movementPaths, dateSortOrder]);

  const dateFilteredPaths = useMemo<SortedPath[]>(() => {
    if (!selectedRange?.from && !selectedRange?.to) {
      return [...sortedMovementPaths].sort((a, b) => {
        if (dateSortOrder === "asc") {
          return a.latestTimestamp - b.latestTimestamp;
        }
        return b.latestTimestamp - a.latestTimestamp;
      });
    }

    return sortedMovementPaths
      .map((path) => {
        const pointsInRange = path.points.filter((point) =>
          isWithinRange(toTimestamp(point.timestamp), selectedRange),
        );

        if (pointsInRange.length === 0) {
          return null;
        }

        return {
          ...path,
          points: pointsInRange,
          latestTimestamp: toTimestamp(
            pointsInRange[pointsInRange.length - 1].timestamp,
          ),
        };
      })
      .filter((path): path is SortedPath => path !== null)
      .sort((a, b) => {
        if (dateSortOrder === "asc") {
          return a.latestTimestamp - b.latestTimestamp;
        }
        return b.latestTimestamp - a.latestTimestamp;
      });
  }, [dateSortOrder, selectedRange, sortedMovementPaths]);

  const dateSessionRows = useMemo(
    () =>
      dateFilteredPaths.map((path) => ({
        sessionId: path.sessionId,
        pointsCount: path.points.length,
        startTimestamp: toTimestamp(path.points[0]?.timestamp ?? ""),
        endTimestamp: toTimestamp(
          path.points[path.points.length - 1]?.timestamp ?? "",
        ),
      })),
    [dateFilteredPaths],
  );

  const totalTablePages = Math.max(
    1,
    Math.ceil(dateSessionRows.length / TABLE_ROWS_PER_PAGE),
  );

  const paginatedDateSessionRows = useMemo(() => {
    const start = (tablePage - 1) * TABLE_ROWS_PER_PAGE;
    const end = start + TABLE_ROWS_PER_PAGE;
    return dateSessionRows.slice(start, end);
  }, [dateSessionRows, tablePage]);

  const sessionMetaById = useMemo(() => {
    return new Map(
      dateFilteredPaths.map((path) => [path.sessionId, path.latestTimestamp]),
    );
  }, [dateFilteredPaths]);

  const sessionIds = useMemo(() => {
    return Array.from(new Set(dateFilteredPaths.map((path) => path.sessionId)));
  }, [dateFilteredPaths]);

  const totalPages = Math.max(
    1,
    Math.ceil(sessionIds.length / SESSION_IDS_PER_PAGE),
  );

  const paginatedSessionIds = useMemo(() => {
    const start = (currentPage - 1) * SESSION_IDS_PER_PAGE;
    const end = start + SESSION_IDS_PER_PAGE;
    return sessionIds.slice(start, end);
  }, [currentPage, sessionIds]);

  useEffect(() => {
    if (!selectedSessionId) return;

    const hasSelectedSession = sessionIds.includes(selectedSessionId);
    if (!hasSelectedSession) {
      setSelectedSessionId(null);
    }
  }, [selectedSessionId, sessionIds]);

  useEffect(() => {
    if (currentPage > totalPages) {
      setCurrentPage(totalPages);
    }
  }, [currentPage, totalPages]);

  useEffect(() => {
    if (tablePage > totalTablePages) {
      setTablePage(totalTablePages);
    }
  }, [tablePage, totalTablePages]);

  useEffect(() => {
    if (!selectedSessionId) return;

    const selectedIndex = sessionIds.indexOf(selectedSessionId);
    if (selectedIndex === -1) return;

    const selectedPage = Math.floor(selectedIndex / SESSION_IDS_PER_PAGE) + 1;

    // Only sync page when selected session or session list changes.
    // Do not force page while user is manually paginating.
    setCurrentPage((prev) => (prev === selectedPage ? prev : selectedPage));
  }, [selectedSessionId, sessionIds]);

  const filteredPaths = useMemo(() => {
    if (!selectedSessionId) {
      return dateFilteredPaths;
    }
    return dateFilteredPaths.filter(
      (path) => path.sessionId === selectedSessionId,
    );
  }, [dateFilteredPaths, selectedSessionId]);

  const pathCoordinates = useMemo(
    () =>
      filteredPaths.flatMap((path) =>
        path.points.map((p) => [p.latitude, p.longitude] as L.LatLngTuple),
      ),
    [filteredPaths],
  );

  const handleRecenterMap = () => {
    const map = mapInstanceRef.current;
    if (!map) return;

    if (pathCoordinates.length > 0) {
      map.fitBounds(L.latLngBounds(pathCoordinates), {
        padding: [40, 40],
        animate: true,
        duration: 0.8,
      });
      return;
    }

    map.flyTo(MAP_CENTER, MAP_ZOOM, { animate: true, duration: 0.8 });
  };

  useEffect(() => {
    if (!mapRef.current) return;
    if (mapInstanceRef.current) return;

    const map = L.map(mapRef.current).setView(MAP_CENTER, MAP_ZOOM);
    const mapLayer = L.tileLayer(
      "https://{s}.basemaps.cartocdn.com/light_all/{z}/{x}/{y}{r}.png",
      {
        attribution:
          '&copy; <a href="https://www.openstreetmap.org/copyright">OSM</a> &copy; <a href="https://carto.com/">CARTO</a>',
      },
    ).addTo(map);

    const satelliteLayer = L.tileLayer(
      "https://server.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile/{z}/{y}/{x}",
      {
        attribution:
          "Tiles &copy; Esri &mdash; Source: Esri, Maxar, Earthstar Geographics",
      },
    );

    tileLayersRef.current = {
      map: mapLayer,
      satellite: satelliteLayer,
    };

    pathLayerRef.current = L.layerGroup().addTo(map);
    mapInstanceRef.current = map;

    return () => {
      map.remove();
      mapInstanceRef.current = null;
      pathLayerRef.current = null;
    };
  }, []);

  useEffect(() => {
    const map = mapInstanceRef.current;
    const tileLayers = tileLayersRef.current;
    if (!map || !tileLayers) return;

    if (baseLayer === "satellite") {
      if (map.hasLayer(tileLayers.map)) {
        map.removeLayer(tileLayers.map);
      }
      if (!map.hasLayer(tileLayers.satellite)) {
        tileLayers.satellite.addTo(map);
      }
      return;
    }

    if (map.hasLayer(tileLayers.satellite)) {
      map.removeLayer(tileLayers.satellite);
    }
    if (!map.hasLayer(tileLayers.map)) {
      tileLayers.map.addTo(map);
    }
  }, [baseLayer]);

  useEffect(() => {
    const map = mapInstanceRef.current;
    const pathLayer = pathLayerRef.current;
    if (!map || !pathLayer) return;

    pathLayer.clearLayers();

    filteredPaths.forEach((path, idx) => {
      const color = PATH_COLORS[idx % PATH_COLORS.length];

      const latlngs = path.points.map(
        (p) => [p.latitude, p.longitude] as L.LatLngTuple,
      );

      if (latlngs.length === 0) {
        return;
      }

      if (latlngs.length > 1) {
        L.polyline(latlngs, {
          color,
          weight: 2,
          opacity: 0.65,
        }).addTo(pathLayer);
      }

      latlngs.forEach((point, pointIdx) => {
        L.circleMarker(point, {
          radius: pointIdx === 0 ? 5 : 6,
          fillColor: color,
          color,
          weight: 2,
          fillOpacity: 0.7,
        })
          .bindPopup(
            `<strong>${path.sessionId.slice(0, 8)}...</strong><br/>Điểm ${pointIdx + 1} / ${latlngs.length}`,
          )
          .addTo(pathLayer);
      });
    });

    if (pathCoordinates.length > 0) {
      map.fitBounds(L.latLngBounds(pathCoordinates), { padding: [40, 40] });
    }
  }, [filteredPaths, pathCoordinates]);

  return (
    <div className="stat-card">
      <div className="mb-4 flex items-center justify-between gap-3">
        <div className="flex items-center gap-2">
          <Route className="h-4 w-4 text-primary" />
          <h3 className="text-sm font-semibold text-foreground">
            Tuyến di chuyển người dùng
          </h3>
        </div>
      </div>

      {movementPaths.length === 0 && (
        <p className="mb-2 text-xs text-muted-foreground">
          Chưa có dữ liệu tuyến di chuyển người dùng
        </p>
      )}

      <div className="grid gap-4 lg:grid-cols-[280px_1fr]">
        <div className="flex h-[620px] flex-col rounded-lg border border-border bg-background p-3">
          <div className="mb-2 rounded-md border border-border p-2">
            <div className="mb-2 text-[11px] font-medium text-muted-foreground">
              Lọc theo khoảng thời gian
            </div>
            <div className="flex items-center gap-2">
              <Popover>
                <PopoverTrigger asChild>
                  <button
                    type="button"
                    className="inline-flex w-full items-center justify-between rounded-md border border-border bg-background px-2 py-1.5 text-xs text-foreground"
                  >
                    <span>{formatRangeLabel(selectedRange)}</span>
                    <CalendarDays className="h-3.5 w-3.5 text-muted-foreground" />
                  </button>
                </PopoverTrigger>
                <PopoverContent align="start" className="w-auto p-0">
                  <Calendar
                    mode="range"
                    selected={selectedRange}
                    onSelect={(range) => {
                      setSelectedRange(range);
                      setCurrentPage(1);
                      setTablePage(1);
                    }}
                    initialFocus
                  />
                </PopoverContent>
              </Popover>

              <button
                type="button"
                onClick={() => {
                  setSelectedRange(undefined);
                  setCurrentPage(1);
                  setTablePage(1);
                }}
                className="rounded-md border border-border px-2 py-1.5 text-xs text-muted-foreground hover:text-foreground"
              >
                Xóa
              </button>
            </div>
          </div>

          <div className="mb-2">
            <span className="text-xs font-medium text-muted-foreground">
              Danh sách phiên
            </span>
          </div>

          <div className="flex min-h-0 flex-1 flex-col">
            <div className="flex-1 space-y-1 overflow-y-auto pr-1">
              <button
                type="button"
                onClick={() => setSelectedSessionId(null)}
                className={`w-full rounded-md border px-2 py-2 text-left text-xs transition-colors ${
                  selectedSessionId === null
                    ? "border-primary bg-primary/10 text-primary"
                    : "border-border bg-background text-muted-foreground hover:text-foreground"
                }`}
              >
                Tất cả phiên
              </button>

              {paginatedSessionIds.map((sessionId) => {
                const isActive = selectedSessionId === sessionId;
                const latestTimestamp = sessionMetaById.get(sessionId) ?? 0;
                return (
                  <button
                    key={sessionId}
                    type="button"
                    onClick={() => setSelectedSessionId(sessionId)}
                    className={`w-full rounded-md border px-2 py-2 text-left text-xs transition-colors ${
                      isActive
                        ? "border-primary bg-primary/10 text-primary"
                        : "border-border bg-background text-muted-foreground hover:text-foreground"
                    }`}
                    title={sessionId}
                  >
                    <span className="mono block truncate">{sessionId}</span>
                    <span className="mt-0.5 block text-[10px] text-muted-foreground">
                      {formatSessionDate(latestTimestamp)}
                    </span>
                  </button>
                );
              })}
            </div>

            {sessionIds.length > 0 && (
              <div className="mt-2 flex items-center justify-between border-t border-border pt-2">
                <button
                  type="button"
                  onClick={() =>
                    setCurrentPage((prev) => Math.max(1, prev - 1))
                  }
                  disabled={currentPage === 1}
                  className="rounded-md border border-border px-2 py-1 text-xs text-muted-foreground disabled:cursor-not-allowed disabled:opacity-50"
                >
                  Trước
                </button>
                <span className="mono text-xs text-muted-foreground">
                  {currentPage}/{totalPages}
                </span>
                <button
                  type="button"
                  onClick={() =>
                    setCurrentPage((prev) => Math.min(totalPages, prev + 1))
                  }
                  disabled={currentPage === totalPages}
                  className="rounded-md border border-border px-2 py-1 text-xs text-muted-foreground disabled:cursor-not-allowed disabled:opacity-50"
                >
                  Sau
                </button>
              </div>
            )}
          </div>
        </div>

        <div className="relative">
          <div ref={mapRef} className="h-[620px] overflow-hidden rounded-lg" />

          <button
            type="button"
            onClick={handleRecenterMap}
            title="Về vị trí hiện tại"
            aria-label="Về vị trí hiện tại"
            className="absolute left-3 top-[96px] z-[500] inline-flex h-[30px] w-[30px] items-center justify-center rounded-[4px] border border-border bg-background text-foreground shadow-sm hover:bg-accent"
          >
            <LocateFixed className="h-3.5 w-3.5" />
          </button>

          <div className="absolute right-3 top-3 z-[500] overflow-hidden rounded-lg border border-border bg-background shadow-sm">
            <button
              type="button"
              onClick={() => setBaseLayer("map")}
              className={`px-3 py-1.5 text-sm ${
                baseLayer === "map"
                  ? "bg-primary text-primary-foreground"
                  : "text-muted-foreground"
              }`}
            >
              Bản đồ
            </button>
            <button
              type="button"
              onClick={() => setBaseLayer("satellite")}
              className={`px-3 py-1.5 text-sm ${
                baseLayer === "satellite"
                  ? "bg-primary text-primary-foreground"
                  : "text-muted-foreground"
              }`}
            >
              Vệ tinh
            </button>
          </div>
        </div>
      </div>

      <div className="mt-4 rounded-lg border border-border bg-background p-3">
        <div className="mb-2 flex items-center justify-between">
          <h4 className="text-sm font-semibold text-foreground">
            Bảng tuyến di chuyển của khách tham quan theo thời gian
          </h4>
          <span className="text-xs text-muted-foreground">
            {selectedRange?.from || selectedRange?.to
              ? `${dateSessionRows.length} phiên trong khoảng đã chọn`
              : `${dateSessionRows.length} phiên`}
          </span>
        </div>

        {(selectedRange?.from || selectedRange?.to) &&
          dateSessionRows.length === 0 && (
            <p className="text-xs text-muted-foreground">
              Không có dữ liệu tuyến di chuyển theo bộ lọc đã chọn.
            </p>
          )}

        {dateSessionRows.length > 0 && (
          <div className="overflow-x-auto">
            <table className="w-full text-left text-xs">
              <thead>
                <tr className="border-b border-border text-muted-foreground">
                  <th className="py-2 pr-3">Mã phiên</th>
                  <th className="py-2 pr-3">Số điểm</th>
                  <th className="py-2 pr-3">Bắt đầu</th>
                  <th className="py-2">Kết thúc</th>
                </tr>
              </thead>
              <tbody>
                {paginatedDateSessionRows.map((row) => (
                  <tr key={row.sessionId} className="border-b border-border/60">
                    <td className="py-2 pr-3 mono">{row.sessionId}</td>
                    <td className="py-2 pr-3">{row.pointsCount}</td>
                    <td className="py-2 pr-3">
                      {formatSessionDate(row.startTimestamp)}
                    </td>
                    <td className="py-2">
                      {formatSessionDate(row.endTimestamp)}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}

        {dateSessionRows.length > 0 && (
          <div className="mt-3 flex items-center justify-between border-t border-border pt-3">
            <button
              type="button"
              onClick={() => setTablePage((prev) => Math.max(1, prev - 1))}
              disabled={tablePage === 1}
              className="rounded-md border border-border px-2 py-1 text-xs text-muted-foreground disabled:cursor-not-allowed disabled:opacity-50"
            >
              Trước
            </button>
            <span className="mono text-xs text-muted-foreground">
              {tablePage}/{totalTablePages}
            </span>
            <button
              type="button"
              onClick={() =>
                setTablePage((prev) => Math.min(totalTablePages, prev + 1))
              }
              disabled={tablePage === totalTablePages}
              className="rounded-md border border-border px-2 py-1 text-xs text-muted-foreground disabled:cursor-not-allowed disabled:opacity-50"
            >
              Sau
            </button>
          </div>
        )}
      </div>
    </div>
  );
}

export default TrajectorySection;
