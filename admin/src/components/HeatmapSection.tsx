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

export function HeatmapSection({
  points = [],
  restaurantPois = [],
}: HeatmapSectionProps) {
  const mapRef = useRef<HTMLDivElement>(null);
  const mapInstanceRef = useRef<L.Map | null>(null);

  // Compute intensity per point (relative to max density)
  const normalizedPoints = useMemo(() => {
    if (points.length === 0) return [];
    // Use simple uniform intensity for raw points (no clustering)
    return points.map((p) => ({ ...p, intensity: 0.6 }));
  }, [points]);

  useEffect(() => {
    if (!mapRef.current || mapInstanceRef.current) return;

    const map = L.map(mapRef.current).setView(MAP_CENTER, MAP_ZOOM);
    L.tileLayer(
      "https://{s}.basemaps.cartocdn.com/light_all/{z}/{x}/{y}{r}.png",
      {
        attribution:
          '&copy; <a href="https://www.openstreetmap.org/copyright">OSM</a> &copy; <a href="https://carto.com/">CARTO</a>',
      },
    ).addTo(map);

    // ── Heatmap circle markers ────────────────────────────────────────────────
    normalizedPoints.forEach(({ latitude, longitude, intensity }) => {
      L.circleMarker([latitude, longitude], {
        radius: 8 + intensity * 14, // 8–22px radius
        fillColor: "hsl(221, 83%, 53%)",
        color: "hsl(221, 83%, 53%)",
        weight: 1,
        opacity: 0.7,
        fillOpacity: 0.3 + intensity * 0.4,
      }).addTo(map);
    });

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
        .addTo(map);
    });

    // Auto-fit bounds if we have real points
    if (normalizedPoints.length > 0) {
      const bounds = L.latLngBounds(
        normalizedPoints.map((p) => [p.latitude, p.longitude] as L.LatLngTuple),
      );
      map.fitBounds(bounds, { padding: [40, 40] });
    }

    mapInstanceRef.current = map;

    return () => {
      map.remove();
      mapInstanceRef.current = null;
    };
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
      <div ref={mapRef} className="h-[400px] rounded-lg overflow-hidden" />
    </div>
  );
}

export default HeatmapSection;
