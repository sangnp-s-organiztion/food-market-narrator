import { useEffect, useMemo, useRef, useState } from "react";
import L from "leaflet";
import "leaflet/dist/leaflet.css";
import { LocateFixed, Route } from "lucide-react";
import type { MovementPath } from "@/types/analytics";

interface TrajectorySectionProps {
  movementPaths?: MovementPath[];
  sessionLimit: 20 | 50 | 100 | 200 | "all";
  onSessionLimitChange: (limit: 20 | 50 | 100 | 200 | "all") => void;
}

const MAP_CENTER: [number, number] = [10.761, 106.703];
const MAP_ZOOM = 16;
const PATH_COLORS = [
  "hsl(221, 83%, 53%)",
  "hsl(199, 89%, 48%)",
  "hsl(142, 71%, 45%)",
  "hsl(280, 67%, 54%)",
  "hsl(25, 95%, 53%)",
];
const SESSION_IDS_PER_PAGE = 10;

export function TrajectorySection({
  movementPaths = [],
  sessionLimit,
  onSessionLimitChange,
}: TrajectorySectionProps) {
  const [baseLayer, setBaseLayer] = useState<"map" | "satellite">("map");
  const [selectedSessionId, setSelectedSessionId] = useState<string | null>(
    null,
  );
  const [currentPage, setCurrentPage] = useState(1);
  const mapRef = useRef<HTMLDivElement>(null);
  const mapInstanceRef = useRef<L.Map | null>(null);
  const pathLayerRef = useRef<L.LayerGroup | null>(null);
  const tileLayersRef = useRef<{
    map: L.TileLayer;
    satellite: L.TileLayer;
  } | null>(null);

  const sessionIds = useMemo(() => {
    return Array.from(new Set(movementPaths.map((path) => path.sessionId)));
  }, [movementPaths]);

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
      return movementPaths;
    }
    return movementPaths.filter((path) => path.sessionId === selectedSessionId);
  }, [movementPaths, selectedSessionId]);

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
        <div className="flex items-center gap-2">
          <span className="text-xs text-muted-foreground">Số phiên</span>
          {[20, 50, 100, 200, "all"].map((limit) => {
            const value = limit as 20 | 50 | 100 | 200 | "all";
            const isActive = sessionLimit === value;
            return (
              <button
                key={value}
                type="button"
                onClick={() => onSessionLimitChange(value)}
                className={`rounded-md border px-2 py-1 text-xs ${
                  isActive
                    ? "border-primary bg-primary text-primary-foreground"
                    : "border-border bg-background text-muted-foreground hover:text-foreground"
                }`}
              >
                {value === "all" ? "Tất cả" : value}
              </button>
            );
          })}
        </div>
      </div>

      {movementPaths.length === 0 && (
        <p className="mb-2 text-xs text-muted-foreground">
          Chưa có dữ liệu tuyến di chuyển người dùng
        </p>
      )}

      <div className="grid gap-4 lg:grid-cols-[280px_1fr]">
        <div className="flex h-[620px] flex-col rounded-lg border border-border bg-background p-3">
          <div className="mb-2 flex items-center justify-between">
            <span className="text-xs font-medium text-muted-foreground">
              Session ID
            </span>
            <span className="text-xs text-muted-foreground mono">
              {filteredPaths.length}/{movementPaths.length}
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
                Tất cả session
              </button>

              {paginatedSessionIds.map((sessionId) => {
                const isActive = selectedSessionId === sessionId;
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
    </div>
  );
}

export default TrajectorySection;
