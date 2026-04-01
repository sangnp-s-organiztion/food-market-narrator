import { useEffect, useRef } from "react";
import { heatmapData, restaurants } from "@/lib/mockData";
import L from "leaflet";
import "leaflet/dist/leaflet.css";

const HeatmapSection = () => {
  const mapRef = useRef<HTMLDivElement>(null);
  const mapInstanceRef = useRef<L.Map | null>(null);

  useEffect(() => {
    if (!mapRef.current || mapInstanceRef.current) return;
    const map = L.map(mapRef.current).setView([16.0, 107.0], 6);
    L.tileLayer("https://{s}.basemaps.cartocdn.com/light_all/{z}/{x}/{y}{r}.png", {
      attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OSM</a> &copy; <a href="https://carto.com/">CARTO</a>',
    }).addTo(map);

    heatmapData.forEach(([lat, lng, intensity]) => {
      L.circleMarker([lat, lng], {
        radius: intensity * 20,
        fillColor: "hsl(221, 83%, 53%)",
        color: "hsl(221, 83%, 53%)",
        weight: 1,
        opacity: 0.7,
        fillOpacity: intensity * 0.5,
      }).addTo(map);
    });

    restaurants.forEach((r) => {
      L.circleMarker([r.latitude, r.longitude], {
        radius: 5,
        fillColor: "hsl(199, 89%, 48%)",
        color: "hsl(199, 89%, 48%)",
        weight: 2,
        fillOpacity: 0.8,
      })
        .bindPopup(`<strong>${r.name}</strong><br/>${r.address}`)
        .addTo(map);
    });

    mapInstanceRef.current = map;
    return () => { map.remove(); mapInstanceRef.current = null; };
  }, []);

  return (
    <div className="stat-card">
      <h3 className="text-sm font-semibold text-foreground mb-4">Bản đồ nhiệt vị trí người dùng nghe âm thanh</h3>
      <div ref={mapRef} className="h-[400px] rounded-lg overflow-hidden" />
    </div>
  );
};

export default HeatmapSection;
