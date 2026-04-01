import { useEffect, useMemo, useRef, useState } from "react";
import L from "leaflet";
import "leaflet.heat";
import "leaflet/dist/leaflet.css";
import { MapPin } from "lucide-react";
import type { HeatmapPoint, TopRestaurant } from "@/types/analytics";

interface HeatmapSectionProps {
  /** Geo points from GET /api/analytics/heatmap */
  points?: HeatmapPoint[];
  /** Restaurant list for POI markers on map */
  restaurantPois?: TopRestaurant[];
  /** Current lookback window (hours) */
  lookbackHours: 1 | 6 | 24 | 168;
  /** Change lookback window */
  onLookbackHoursChange: (hours: 1 | 6 | 24 | 168) => void;
}

// Vinh Khanh Food Street — center of data (Ho Chi Minh City, District 4)
const MAP_CENTER: [number, number] = [10.761, 106.703];
const MAP_ZOOM = 16;
const HEAT_PALETTE_7 = [
  "#0d1b8f",
  "#1f5fff",
  "#00b5ff",
  "#29d66f",
  "#e6f34b",
  "#ff9f1a",
  "#ff3b30",
];
const PATH_COLORS = [
  "hsl(221, 83%, 53%)",
  "hsl(199, 89%, 48%)",
  "hsl(142, 71%, 45%)",
  "hsl(280, 67%, 54%)",
  "hsl(25, 95%, 53%)",
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

export function HeatmapSection({
  points = [],
  restaurantPois = [],
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

  const poiCoordinates = useMemo(() => {
    type PoiWithCoords = TopRestaurant & {
      latitude?: number;
      longitude?: number;
    };

    return (restaurantPois as PoiWithCoords[])
      .filter(
        (r) =>
          typeof r.latitude === "number" && typeof r.longitude === "number",
      )
      .map(
        (r) => [r.latitude as number, r.longitude as number] as L.LatLngTuple,
      );
  }, [restaurantPois]);

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

    restaurantPois.forEach((r) => {
      const lat = (r as TopRestaurant & { latitude?: number }).latitude;
      const lng = (r as TopRestaurant & { longitude?: number }).longitude;
      if (typeof lat !== "number" || typeof lng !== "number") return;
      const name = r.restaurantName ?? "";

      L.circleMarker([lat, lng], {
        radius: 5,
        fillColor: "hsl(199, 89%, 48%)",
        color: "hsl(199, 89%, 48%)",
        weight: 2,
        fillOpacity: 0.8,
      })
        .bindPopup(
          `<strong>${name}</strong><br/><span style="color:#666">${r.playCount ? `${r.playCount} lượt nghe` : ""}</span>`,
        )
        .addTo(poiLayer);
    });

    if (weightedHeatPoints.length > 0) {
      const bounds = L.latLngBounds(
        weightedHeatPoints.map((p) => [p[0], p[1]] as L.LatLngTuple),
      );
      map.fitBounds(bounds, { padding: [40, 40] });
    }
  }, [weightedHeatPoints, activeGradient, restaurantPois]);

  return (
    <div className="stat-card">
      <div className="mb-4 flex items-center justify-between gap-3">
        <h3 className="text-sm font-semibold text-foreground">
          Bản đồ nhiệt vị trí người dùng nghe âm thanh
        </h3>
        <div className="flex items-center gap-2">
          <span className="text-xs text-muted-foreground">
            Khoảng thời gian
          </span>
          {[1, 6, 24, 168].map((h) => {
            const value = h as 1 | 6 | 24 | 168;
            const isActive = lookbackHours === value;
            const label = value === 168 ? "7d" : `${value}h`;
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
