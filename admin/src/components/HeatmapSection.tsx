import { useEffect, useRef, useMemo } from "react";
import L from "leaflet";
import "leaflet/dist/leaflet.css";
import type { HeatmapPoint, TopRestaurant } from "@/types/analytics";

interface HeatmapSectionProps {
  /** Geo points from GET /api/analytics/heatmap */
  points?: HeatmapPoint[];
  /** Restaurant list for POI markers on map */
  restaurantPois?: TopRestaurant[];
}

// Vinh Khanh Food Street — center of data (Ho Chi Minh City, District 4)
const MAP_CENTER: [number, number] = [10.761, 106.703];
const MAP_ZOOM = 16;
const GRID_DECIMALS = 4; // ~11m grid at equator, enough for hotspot grouping

const HEAT_RAMP = [
  { t: 0.0, color: "#0000ff" }, // blue
  { t: 0.2, color: "#00ffff" }, // cyan
  { t: 0.4, color: "#00ff00" }, // green
  { t: 0.7, color: "#ffff00" }, // yellow
  { t: 0.9, color: "#ff0000" }, // red
  { t: 1.0, color: "#ffffff" }, // white (hottest)
];

function clamp01(value: number): number {
  return Math.max(0, Math.min(1, value));
}

function hexToRgb(hex: string): [number, number, number] {
  const normalized = hex.replace("#", "");
  const value = parseInt(normalized, 16);
  return [(value >> 16) & 255, (value >> 8) & 255, value & 255];
}

function rgbToHex(r: number, g: number, b: number): string {
  const toHex = (v: number) => Math.round(v).toString(16).padStart(2, "0");
  return `#${toHex(r)}${toHex(g)}${toHex(b)}`;
}

function interpolateColor(t: number): string {
  const normalized = clamp01(t);
  for (let i = 0; i < HEAT_RAMP.length - 1; i += 1) {
    const a = HEAT_RAMP[i];
    const b = HEAT_RAMP[i + 1];
    if (normalized >= a.t && normalized <= b.t) {
      const localT = (normalized - a.t) / Math.max(0.0001, b.t - a.t);
      const [ar, ag, ab] = hexToRgb(a.color);
      const [br, bg, bb] = hexToRgb(b.color);
      return rgbToHex(
        ar + (br - ar) * localT,
        ag + (bg - ag) * localT,
        ab + (bb - ab) * localT,
      );
    }
  }
  return HEAT_RAMP[HEAT_RAMP.length - 1].color;
}

function makeGridKey(lat: number, lng: number): string {
  return `${lat.toFixed(GRID_DECIMALS)}:${lng.toFixed(GRID_DECIMALS)}`;
}

export function HeatmapSection({
  points = [],
  restaurantPois = [],
}: HeatmapSectionProps) {
  const mapRef = useRef<HTMLDivElement>(null);
  const mapInstanceRef = useRef<L.Map | null>(null);
  const heatLayerRef = useRef<L.LayerGroup | null>(null);
  const poiLayerRef = useRef<L.LayerGroup | null>(null);

  // Bin GPS points by small grid to render density hotspots with a color ramp.
  const normalizedPoints = useMemo(() => {
    if (points.length === 0) return [];

    const buckets = new Map<
      string,
      { latitude: number; longitude: number; count: number }
    >();
    points.forEach((p) => {
      const key = makeGridKey(p.latitude, p.longitude);
      const current = buckets.get(key);
      if (current) {
        current.count += 1;
        return;
      }
      buckets.set(key, {
        latitude: p.latitude,
        longitude: p.longitude,
        count: 1,
      });
    });

    const clustered = Array.from(buckets.values());
    const maxCount = Math.max(...clustered.map((p) => p.count), 1);

    return clustered.map((p) => ({
      latitude: p.latitude,
      longitude: p.longitude,
      count: p.count,
      intensity: clamp01(p.count / maxCount),
      color: interpolateColor(p.count / maxCount),
    }));
  }, [points]);

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

    heatLayerRef.current = L.layerGroup().addTo(map);
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
    const heatLayer = heatLayerRef.current;
    const poiLayer = poiLayerRef.current;
    if (!map || !heatLayer || !poiLayer) return;

    heatLayer.clearLayers();
    poiLayer.clearLayers();

    // ── Heatmap circle markers ────────────────────────────────────────────────
    normalizedPoints.forEach(
      ({ latitude, longitude, intensity, color, count }) => {
        L.circleMarker([latitude, longitude], {
          radius: 7 + intensity * 20,
          fillColor: color,
          color,
          weight: 1,
          opacity: 0.6,
          fillOpacity: 0.18 + intensity * 0.52,
        })
          .bindTooltip(`Mật độ: ${count} điểm`, {
            direction: "top",
            opacity: 0.9,
          })
          .addTo(heatLayer);
      },
    );

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
    if (normalizedPoints.length > 0) {
      const bounds = L.latLngBounds(
        normalizedPoints.map((p) => [p.latitude, p.longitude] as L.LatLngTuple),
      );
      map.fitBounds(bounds, { padding: [40, 40] });
    }
  }, [normalizedPoints, restaurantPois]);

  return (
    <div className="stat-card">
      <h3 className="text-sm font-semibold text-foreground mb-4">
        Bản đồ nhiệt vị trí người dùng nghe âm thanh
      </h3>
      {points.length === 0 && (
        <p className="text-xs text-muted-foreground mb-2">
          Chưa có dữ liệu GPS — hiển thị vị trí nhà hàng tham chiếu
        </p>
      )}
      {points.length > 0 && (
        <div className="mb-2 flex items-center gap-2 text-[11px] text-muted-foreground">
          <span>Lạnh</span>
          <div
            className="h-2 w-44 rounded-sm"
            style={{
              background:
                "linear-gradient(to right, #0000ff 0%, #00ffff 20%, #00ff00 40%, #ffff00 70%, #ff0000 90%, #ffffff 100%)",
            }}
          />
          <span>Nóng</span>
        </div>
      )}
      <div ref={mapRef} className="h-[400px] rounded-lg overflow-hidden" />
    </div>
  );
}

export default HeatmapSection;
