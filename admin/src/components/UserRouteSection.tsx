import { useEffect, useRef } from "react";
import { userPaths } from "@/lib/mockData";
import L from "leaflet";
import "leaflet/dist/leaflet.css";

const UserRouteSection = () => {
  const mapRef = useRef<HTMLDivElement>(null);
  const mapInstanceRef = useRef<L.Map | null>(null);

  useEffect(() => {
    if (!mapRef.current || mapInstanceRef.current) return;
    const map = L.map(mapRef.current).setView([16.0, 107.0], 6);
    L.tileLayer("https://{s}.basemaps.cartocdn.com/light_all/{z}/{x}/{y}{r}.png", {
      attribution: '&copy; OSM &copy; CARTO',
    }).addTo(map);

    const colors = ["hsl(221, 83%, 53%)", "hsl(199, 89%, 48%)"];
    userPaths.forEach((path, idx) => {
      const latlngs = path.points.map((p) => [p.lat, p.lng] as [number, number]);
      L.polyline(latlngs, { color: colors[idx % colors.length], weight: 2, opacity: 0.6 }).addTo(map);
      path.points.forEach((p) => {
        const isPulse = p.duration > 60;
        L.circleMarker([p.lat, p.lng], {
          radius: isPulse ? 8 : 5,
          fillColor: colors[idx % colors.length],
          color: colors[idx % colors.length],
          weight: 2,
          fillOpacity: 0.7,
        })
          .bindPopup(`<strong>${p.restaurant}</strong><br/>${path.username}<br/>Thời gian: ${p.duration}s`)
          .addTo(map);
      });
    });

    mapInstanceRef.current = map;
    return () => { map.remove(); mapInstanceRef.current = null; };
  }, []);

  return (
    <div className="stat-card">
      <h3 className="text-sm font-semibold text-foreground mb-4">Tuyến di chuyển người dùng</h3>
      <div className="flex gap-3 mb-3">
        {userPaths.map((p) => (
          <span key={p.userId} className="text-xs px-2 py-1 rounded-full bg-muted font-medium">
            {p.username} — {p.points.length} điểm
          </span>
        ))}
      </div>
      <div ref={mapRef} className="h-[400px] rounded-lg overflow-hidden" />
    </div>
  );
};

export default UserRouteSection;
