import { useEffect, useMemo, useRef, useState } from "react";
import L from "leaflet";
import "leaflet.heat";
import "leaflet/dist/leaflet.css";
import { MapPin } from "lucide-react";
import type { HeatmapPoint } from "@/types/analytics";

interface HeatmapPoi {
  restaurantId: string;
  restaurantName: string;
  latitude: number | null;
  longitude: number | null;
  playCount?: number;
}

interface HeatmapSectionProps {
  /** Geo points from GET /api/analytics/heatmap */
  points?: HeatmapPoint[];
  /** POI list for markers and GPS-point density assignment */
  poiList?: HeatmapPoi[];
  /** Current lookback window (hours) */
  lookbackHours: 1 | 6 | 24 | "all";
  /** Change lookback window */
  onLookbackHoursChange: (hours: 1 | 6 | 24 | "all") => void;
}

// Vinh Khanh Food Street — center of data (Ho Chi Minh City, District 4)
const MAP_CENTER: [number, number] = [10.761, 106.703];
const MAP_ZOOM = 16;
const POI_ASSIGNMENT_RADIUS_METERS = 120;
const HEAT_PALETTE_7 = [
  "#0d1b8f",
  "#1f5fff",
  "#00b5ff",
  "#29d66f",
  "#e6f34b",
  "#ff9f1a",
  "#ff3b30",
];
function paletteToGradient(palette: string[]): Record<number, string> {
  if (palette.length === 1) {
    return { 0: palette[0], 1: palette[0] };
  }

  const step = 1 / (palette.length - 1);
  const gradient: Record<number, string> = {};
  palette.forEach((color, idx) => {
    const key = Number((idx * step).toFixed(2));
    gradient[key] = color;
  });

  return gradient;
}

function distanceMeters(
  lat1: number,
  lng1: number,
  lat2: number,
  lng2: number,
): number {
  const toRad = (deg: number) => (deg * Math.PI) / 180;
  const earthRadiusMeters = 6371000;
  const dLat = toRad(lat2 - lat1);
  const dLng = toRad(lng2 - lng1);
  const a =
    Math.sin(dLat / 2) * Math.sin(dLat / 2) +
    Math.cos(toRad(lat1)) *
      Math.cos(toRad(lat2)) *
      Math.sin(dLng / 2) *
      Math.sin(dLng / 2);
  const c = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));
  return earthRadiusMeters * c;
}

export function HeatmapSection({
  points = [],
  poiList = [],
  lookbackHours,
  onLookbackHoursChange,
}: HeatmapSectionProps) {
  const [baseLayer, setBaseLayer] = useState<"map" | "satellite">("map");
  const mapRef = useRef<HTMLDivElement>(null);
  const mapInstanceRef = useRef<L.Map | null>(null);
  const heatLayerRef = useRef<L.HeatLayer | null>(null);
  const poiLayerRef = useRef<L.LayerGroup | null>(null);
  const tileLayersRef = useRef<{
    map: L.TileLayer;
    satellite: L.TileLayer;
  } | null>(null);

  const activePalette = HEAT_PALETTE_7;
  const activeGradient = paletteToGradient(activePalette);

  const weightedHeatPoints = useMemo(
    () =>
      points.map((p) => {
        const rawIntensity =
          typeof p.intensity === "number" ? p.intensity : 0.22;
        const intensity = Math.max(0.05, Math.min(rawIntensity, 1));
        return [p.latitude, p.longitude, intensity] as [number, number, number];
      }),
    [points],
  );

  const poisWithCoords = useMemo(
    () =>
      poiList.filter(
        (r) =>
          typeof r.latitude === "number" && typeof r.longitude === "number",
      ) as Array<HeatmapPoi & { latitude: number; longitude: number }>,
    [poiList],
  );

  const poiCoordinates = useMemo(
    () => poisWithCoords.map((r) => [r.latitude, r.longitude] as L.LatLngTuple),
    [poisWithCoords],
  );

  const poiGpsPointCount = useMemo(() => {
    const counts = new Map<string, number>();
    if (weightedHeatPoints.length === 0 || poisWithCoords.length === 0) {
      return counts;
    }

    for (const [lat, lng] of weightedHeatPoints) {
      let nearestPoi:
        | (HeatmapPoi & { latitude: number; longitude: number })
        | null = null;
      let nearestDistance = Number.POSITIVE_INFINITY;

      for (const poi of poisWithCoords) {
        const d = distanceMeters(lat, lng, poi.latitude, poi.longitude);
        if (d < nearestDistance) {
          nearestDistance = d;
          nearestPoi = poi;
        }
      }

      if (nearestPoi && nearestDistance <= POI_ASSIGNMENT_RADIUS_METERS) {
        counts.set(
          nearestPoi.restaurantId,
          (counts.get(nearestPoi.restaurantId) ?? 0) + 1,
        );
      }
    }

    return counts;
  }, [weightedHeatPoints, poisWithCoords]);

  const poiMarkerData = useMemo(() => {
    const rows = poisWithCoords.map((poi) => ({
      ...poi,
      gpsPointCount: poiGpsPointCount.get(poi.restaurantId) ?? 0,
    }));
    return rows.sort((a, b) => b.gpsPointCount - a.gpsPointCount);
  }, [poisWithCoords, poiGpsPointCount]);

  const maxGpsPointCount = useMemo(
    () => Math.max(...poiMarkerData.map((x) => x.gpsPointCount), 0),
    [poiMarkerData],
  );

  const handleRecenterMap = () => {
    const map = mapInstanceRef.current;
    if (!map) return;

    if (weightedHeatPoints.length > 0) {
      const bounds = L.latLngBounds(
        weightedHeatPoints.map((p) => [p[0], p[1]] as L.LatLngTuple),
      );
      map.fitBounds(bounds, {
        padding: [40, 40],
        animate: true,
        duration: 0.8,
      });
      return;
    }

    if (poiCoordinates.length > 0) {
      map.fitBounds(L.latLngBounds(poiCoordinates), {
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

    poiLayerRef.current = L.layerGroup().addTo(map);

    mapInstanceRef.current = map;

    return () => {
      map.remove();
      mapInstanceRef.current = null;
      heatLayerRef.current = null;
      poiLayerRef.current = null;
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
    const poiLayer = poiLayerRef.current;
    if (!map || !poiLayer) return;

    poiLayer.clearLayers();

    if (heatLayerRef.current) {
      map.removeLayer(heatLayerRef.current);
      heatLayerRef.current = null;
    }

    if (weightedHeatPoints.length > 0) {
      const layer = L.heatLayer(weightedHeatPoints, {
        radius: 30,
        blur: 24,
        maxZoom: 19,
        minOpacity: 0.08,
        max: 0.95,
        gradient: activeGradient,
      }).addTo(map);
      heatLayerRef.current = layer;
    }

    poiMarkerData.forEach((poi) => {
      const ratio =
        maxGpsPointCount > 0 ? poi.gpsPointCount / maxGpsPointCount : 0;
      const radius = 5 + Math.round(ratio * 8);
      const fillColor =
        poi.gpsPointCount > 0 ? "hsl(12, 92%, 56%)" : "hsl(199, 89%, 48%)";

      const marker = L.circleMarker([poi.latitude, poi.longitude], {
        radius,
        fillColor,
        color: "#ffffff",
        weight: 1.5,
        fillOpacity: poi.gpsPointCount > 0 ? 0.85 : 0.45,
      }).addTo(poiLayer);

      marker.bindPopup(
        [
          `<strong>${poi.restaurantName ?? "(Không tên)"}</strong>`,
          typeof poi.playCount === "number"
            ? `<br/><span style="color:#666">Lượt nghe: ${poi.playCount}</span>`
            : "",
        ].join(""),
      );
    });

    if (weightedHeatPoints.length > 0) {
      const bounds = L.latLngBounds(
        weightedHeatPoints.map((p) => [p[0], p[1]] as L.LatLngTuple),
      );
      map.fitBounds(bounds, {
        padding: [40, 40],
        animate: true,
        duration: 0.8,
      });
      return;
    }

    if (poiCoordinates.length > 0) {
      map.fitBounds(L.latLngBounds(poiCoordinates), {
        padding: [40, 40],
        animate: true,
        duration: 0.8,
      });
    }
  }, [
    weightedHeatPoints,
    activeGradient,
    poiMarkerData,
    maxGpsPointCount,
    poiCoordinates,
  ]);

  return (
    <div className="stat-card">
      <div className="mb-4 flex items-center justify-between gap-3">
        <h3 className="text-sm font-semibold text-foreground">
          Bản đồ nhiệt vị trí người dùng
        </h3>
        <div className="flex items-center gap-2">
          <span className="text-xs text-muted-foreground">
            Khoảng thời gian
          </span>
          {[1, 6, 24, "all"].map((h) => {
            const value = h as 1 | 6 | 24 | "all";
            const isActive = lookbackHours === value;
            const label = value === "all" ? "Tất cả" : `${value}h`;
            return (
              <button
                key={value}
                type="button"
                onClick={() => onLookbackHoursChange(value)}
                className={`rounded-md border px-2 py-1 text-xs ${
                  isActive
                    ? "border-primary bg-primary text-primary-foreground"
                    : "border-border bg-background text-muted-foreground hover:text-foreground"
                }`}
              >
                {label}
              </button>
            );
          })}
        </div>
      </div>

      {points.length === 0 && (
        <p className="text-xs text-muted-foreground mb-2">
          Chưa có dữ liệu GPS — hiển thị vị trí nhà hàng tham chiếu
        </p>
      )}
      {points.length > 0 && (
        <div className="mb-2 flex items-center gap-2 text-[11px] text-muted-foreground">
          <span>Lạnh</span>
          <div
            className="h-2 w-52 overflow-hidden rounded-sm border border-border"
            style={{
              background: `linear-gradient(90deg, ${activePalette.join(", ")})`,
            }}
          />
          <div className="flex items-center gap-1 text-[10px]">
            <span>Nóng</span>
            {/* <span className="text-muted-foreground/70">(loang mượt)</span> */}
          </div>
          {/* <span>(7 mức)</span> */}
        </div>
      )}

      <div className="relative">
        <div ref={mapRef} className="h-[460px] rounded-lg overflow-hidden" />

        <button
          type="button"
          onClick={handleRecenterMap}
          title="Về vị trí hiện tại"
          aria-label="Về vị trí hiện tại"
          className="absolute left-3 top-[96px] z-[500] inline-flex h-[30px] w-[30px] items-center justify-center rounded-[4px] border border-border bg-background text-foreground shadow-sm hover:bg-accent"
        >
          <MapPin className="h-3.5 w-3.5" />
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
  );
}

export default HeatmapSection;
