import { useEffect, useRef } from "react";
import L from "leaflet";
import "leaflet/dist/leaflet.css";
import type { MovementPath } from "@/types/analytics";

interface UserRouteSectionProps {
  /** Movement paths from GET /api/analytics/movement-paths */
  paths?: MovementPath[];
}

const MAP_CENTER: [number, number] = [10.761, 106.703];
const MAP_ZOOM = 16;

// Cycle through these colors for different sessions
const PATH_COLORS = [
  "hsl(221, 83%, 53%)",
  "hsl(199, 89%, 48%)",
  "hsl(142, 71%, 45%)",
  "hsl(280, 67%, 54%)",
  "hsl(25, 95%, 53%)",
];

function formatDuration(seconds: number): string {
  if (seconds < 60) return `${seconds}s`;
  const m = Math.floor(seconds / 60);
  const s = seconds % 60;
  return s > 0 ? `${m}m ${s}s` : `${m}m`;
}

export function UserRouteSection({ paths = [] }: UserRouteSectionProps) {
  const mapRef = useRef<HTMLDivElement>(null);
  const mapInstanceRef = useRef<L.Map | null>(null);

  useEffect(() => {
    if (!mapRef.current || mapInstanceRef.current) return;

    const map = L.map(mapRef.current).setView(MAP_CENTER, MAP_ZOOM);
    L.tileLayer(
      "https://{s}.basemaps.cartocdn.com/light_all/{z}/{x}/{y}{r}.png",
      {
        attribution: "&copy; OSM &copy; CARTO",
      }
    ).addTo(map);

    if (paths.length === 0) {
      // No paths yet — just show empty map centered on the food street
      mapInstanceRef.current = map;
      return;
    }

    // Collect all bounds to auto-fit
    const allPoints: L.LatLngTuple[] = [];

    paths.forEach((path, idx) => {
      const color = PATH_COLORS[idx % PATH_COLORS.length];

      // Draw polyline connecting all points in order
      const latlngs = path.points.map((p) => {
        const tuple: L.LatLngTuple = [p.latitude, p.longitude];
        allPoints.push(tuple);
        return tuple;
      });

      if (latlngs.length < 2) {
        // Single point — add marker only
        if (latlngs.length === 1) {
          L.circleMarker(latlngs[0], {
            radius: 6,
            fillColor: color,
            color,
            weight: 2,
            fillOpacity: 0.7,
          })
            .bindPopup(
              `<strong>${path.sessionId.slice(0, 8)}…</strong><br/>1 điểm dừng`
            )
            .addTo(map);
        }
        return;
      }

      // Polyline
      L.polyline(latlngs, {
        color,
        weight: 2,
        opacity: 0.6,
      }).addTo(map);

      // Numbered circle markers at each stop
      path.points.forEach((pt, ptIdx) => {
        const isPulse = pt.longitude !== latlngs[0][1]; // first point in path
        L.circleMarker([pt.latitude, pt.longitude], {
          radius: isPulse ? 7 : 5,
          fillColor: color,
          color,
          weight: 2,
          fillOpacity: 0.7,
        })
          .bindPopup(
            `<strong>${path.sessionId.slice(0, 8)}…</strong><br/>` +
              `Điểm ${ptIdx + 1} / ${path.points.length}<br/>` +
              `${new Date(pt.timestamp).toLocaleString("vi-VN")}`
          )
          .addTo(map);
      });
    });

    // Fit map to show all paths
    if (allPoints.length > 0) {
      const bounds = L.latLngBounds(allPoints);
      map.fitBounds(bounds, { padding: [40, 40] });
    }

    mapInstanceRef.current = map;

    return () => {
      map.remove();
      mapInstanceRef.current = null;
    };
  }, [paths]);

  return (
    <div className="stat-card">
      <h3 className="text-sm font-semibold text-foreground mb-4">
        Tuyến di chuyển người dùng
      </h3>

      {paths.length > 0 && (
        <div className="flex flex-wrap gap-2 mb-3">
          {paths.slice(0, 5).map((p, idx) => (
            <span
              key={p.sessionId}
              className="text-xs px-2 py-1 rounded-full bg-muted font-medium"
            >
              <span
                style={{
                  display: "inline-block",
                  width: 8,
                  height: 8,
                  borderRadius: "50%",
                  background: PATH_COLORS[idx % PATH_COLORS.length],
                  marginRight: 4,
                  verticalAlign: "middle",
                }}
              />
              {p.sessionId.slice(0, 8)}… — {p.points.length} điểm
            </span>
          ))}
          {paths.length > 5 && (
            <span className="text-xs px-2 py-1 text-muted-foreground">
              +{paths.length - 5} tuyến khác
            </span>
          )}
        </div>
      )}

      {paths.length === 0 && (
        <p className="text-xs text-muted-foreground mb-2">
          Chưa có dữ liệu tuyến di chuyển
        </p>
      )}

      <div ref={mapRef} className="h-[400px] rounded-lg overflow-hidden" />
    </div>
  );
}

export default UserRouteSection;
