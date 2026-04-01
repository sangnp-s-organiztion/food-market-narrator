import { useEffect, useMemo, useRef, useState } from "react";
import L from "leaflet";
import "leaflet.heat";
import "leaflet/dist/leaflet.css";
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
const HEAT_PALETTE_5 = ["#0000ff", "#00ffff", "#00ff00", "#ffff00", "#ff0000"];
const HEAT_PALETTE_7 = [
  "#000000",
  "#0000ff",
  "#00ffff",
  "#00ff00",
  "#ffff00",
  "#ff0000",
  "#ffffff",
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
  const [paletteMode, setPaletteMode] = useState<5 | 7>(7);
  const mapRef = useRef<HTMLDivElement>(null);
  const mapInstanceRef = useRef<L.Map | null>(null);
  const heatLayerRef = useRef<L.HeatLayer | null>(null);
  const poiLayerRef = useRef<L.LayerGroup | null>(null);

  const activePalette = paletteMode === 7 ? HEAT_PALETTE_7 : HEAT_PALETTE_5;
  const activeGradient = useMemo(
    () => paletteToGradient(activePalette),
    [activePalette],
  );

  const weightedHeatPoints = useMemo(
    () =>
      points.map(
        (p) =>
          [p.latitude, p.longitude, p.intensity ?? 0.6] as [
            number,
            number,
            number,
          ],
      ),
    [points],
  );

  useEffect(() => {
    if (!mapRef.current) return;
    if (mapInstanceRef.current) return;

    const map = L.map(mapRef.current).setView(MAP_CENTER, MAP_ZOOM);
    L.tileLayer(
      "https://{s}.basemaps.cartocdn.com/light_all/{z}/{x}/{y}{r}.png",
      {
        attribution:
          '&copy; <a href="https://www.openstreetmap.org/copyright">OSM</a> &copy; <a href="https://carto.com/">CARTO</a>',
      },
    ).addTo(map);

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
        blur: 26,
        maxZoom: 18,
        minOpacity: 0.22,
        gradient: activeGradient,
      }).addTo(map);
      heatLayerRef.current = layer;
    }

    // ── POI restaurant markers ───────────────────────────────────────────────
    // Only render POI markers when coordinates are available from API payload.
    type PoiWithCoords = TopRestaurant & {
      latitude?: number;
      longitude?: number;
    };
    const poisWithCoords = (restaurantPois as PoiWithCoords[]).filter(
      (r) => typeof r.latitude === "number" && typeof r.longitude === "number",
    );
    poisWithCoords.forEach((r) => {
      const lat = r.latitude as number;
      const lng = r.longitude as number;
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

    // Auto-fit bounds if we have real points
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

      <div className="mb-2 flex items-center gap-2">
        <span className="text-xs text-muted-foreground">Ramp màu</span>
        {[5, 7].map((mode) => {
          const isActive = paletteMode === mode;
          return (
            <button
              key={mode}
              type="button"
              onClick={() => setPaletteMode(mode as 5 | 7)}
              className={`rounded-md border px-2 py-1 text-xs ${
                isActive
                  ? "border-primary bg-primary text-primary-foreground"
                  : "border-border bg-background text-muted-foreground hover:text-foreground"
              }`}
            >
              {mode} mức
            </button>
          );
        })}
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
            <span>{paletteMode} mức</span>
            <span className="text-muted-foreground/70">(loang mượt)</span>
          </div>
          <span>Nóng</span>
        </div>
      )}
      <div ref={mapRef} className="h-[400px] rounded-lg overflow-hidden" />
    </div>
  );
}

export default HeatmapSection;
