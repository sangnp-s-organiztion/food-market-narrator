import { useEffect, useRef } from "react";
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

    mapInstanceRef.current = map;
    return () => { map.remove(); mapInstanceRef.current = null; };
  }, []);

  return (
    <div className="stat-card">
      <h3 className="text-sm font-semibold text-foreground mb-4">Tuyến di chuyển người dùng</h3>
      <div className="flex gap-3 mb-3">
        <span className="text-xs px-2 py-1 rounded-full bg-muted font-medium text-muted-foreground">
          Chưa có dữ liệu tuyến di chuyển
        </span>
      </div>
      <div ref={mapRef} className="h-[400px] rounded-lg overflow-hidden" />
    </div>
  );
};

export default UserRouteSection;
