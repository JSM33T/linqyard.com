"use client";

import { useEffect, useMemo, useState } from 'react';
import dynamic from 'next/dynamic';
import type { LatLngExpression } from 'leaflet';
import type { FeatureCollection } from 'geojson';
import 'leaflet/dist/leaflet.css';

// Dynamically import map components to avoid SSR issues
const MapContainer = dynamic(
  () => import('react-leaflet').then((mod) => mod.MapContainer),
  { ssr: false }
);

const TileLayer = dynamic(
  () => import('react-leaflet').then((mod) => mod.TileLayer),
  { ssr: false }
);

const GeoJSON = dynamic(
  () => import('react-leaflet').then((mod) => mod.GeoJSON),
  { ssr: false }
);

const CircleMarker = dynamic(
  () => import('react-leaflet').then((mod) => mod.CircleMarker),
  { ssr: false }
);

const Popup = dynamic(
  () => import('react-leaflet').then((mod) => mod.Popup),
  { ssr: false }
);

// Centralized theme configuration for easy customization
export const mapTheme = {
  // Marker colors (orange theme)
  markerFill: '#fb923c',     // orange-400
  markerStroke: '#c2410c',   // orange-700
  markerFillOpacity: 0.6,
  markerWeight: 2,
  // Country border colors
  borderColor: '#f97316',    // orange-500
  borderWeight: 1.2,
  borderFillOpacity: 0,      // transparent fill - borders only
  // Legend colors (match marker colors)
  legendBg: 'bg-orange-400',
  legendBorder: 'border-orange-700',
};

interface WorldMapProps {
  data: Array<{
    country: string;
    city?: string | null;
    count: number;
    latitude?: number | null;
    longitude?: number | null;
  }>;
  height?: number;
  theme?: Partial<typeof mapTheme>;
}

export function WorldMap({ data, height = 400, theme }: WorldMapProps) {
  // Merge custom theme with defaults
  const activeTheme = useMemo(() => ({ ...mapTheme, ...theme }), [theme]);
  
  // State for GeoJSON data (loaded client-side to avoid SSR issues)
  const [geoJsonData, setGeoJsonData] = useState<FeatureCollection | null>(null);

  const points = useMemo(() => 
    data.filter(item => item.latitude != null && item.longitude != null)
      .map(item => ({
        position: [item.latitude!, item.longitude!] as LatLngExpression,
        count: item.count,
        label: item.city ? `${item.city}, ${item.country}` : item.country,
        latitude: item.latitude!,
        longitude: item.longitude!
      })),
    [data]
  );

  const maxCount = useMemo(() => 
    Math.max(...points.map(p => p.count), 1),
    [points]
  );

  // Fix Leaflet icon issue in Next.js
  useEffect(() => {
    if (typeof window !== 'undefined') {
      const L = require('leaflet');
      delete (L.Icon.Default.prototype as any)._getIconUrl;
      L.Icon.Default.mergeOptions({
        iconRetinaUrl: 'https://cdnjs.cloudflare.com/ajax/libs/leaflet/1.7.1/images/marker-icon-2x.png',
        iconUrl: 'https://cdnjs.cloudflare.com/ajax/libs/leaflet/1.7.1/images/marker-icon.png',
        shadowUrl: 'https://cdnjs.cloudflare.com/ajax/libs/leaflet/1.7.1/images/marker-shadow.png',
      });
    }
  }, []);

  // Load GeoJSON data client-side only
  useEffect(() => {
    fetch('/geo/world-countries.geo.json')
      .then(res => res.json())
      .then(data => setGeoJsonData(data))
      .catch(err => console.warn('Failed to load country borders:', err));
  }, []);

  if (points.length === 0) {
    return (
      <div className="flex items-center justify-center rounded-lg border border-border bg-muted/20" style={{ height: `${height}px` }}>
        <p className="text-sm text-muted-foreground">No location data with coordinates available</p>
      </div>
    );
  }

  return (
    <div className="relative">
      <div style={{ height: `${height}px` }} className="rounded-lg overflow-hidden border border-border">
        <MapContainer
          center={[20, 0]}
          zoom={2}
          style={{ height: '100%', width: '100%' }}
          scrollWheelZoom={true}
        >
          {/* Layer 1: Neutral base tiles (CARTO light) */}
          <TileLayer
            attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> &copy; <a href="https://carto.com/attributions">CARTO</a>'
            url="https://{s}.basemaps.cartocdn.com/light_all/{z}/{x}/{y}{r}.png"
          />
          
          {/* Layer 2: Country borders (GeoJSON) - renders above tiles, below markers */}
          {geoJsonData && (
            <GeoJSON
              data={geoJsonData}
              style={{
                color: activeTheme.borderColor,
                weight: activeTheme.borderWeight,
                fillOpacity: activeTheme.borderFillOpacity,
              }}
            />
          )}
          
          {/* Layer 3: Data markers - renders above borders */}
          {points.map((point, idx) => {
            // Scale radius based on count (min 5px, max 25px)
            const minRadius = 5;
            const maxRadius = 25;
            const radius = minRadius + (point.count / maxCount) * (maxRadius - minRadius);

            return (
              <CircleMarker
                key={idx}
                center={point.position}
                radius={radius}
                pathOptions={{
                  fillColor: activeTheme.markerFill,
                  fillOpacity: activeTheme.markerFillOpacity,
                  color: activeTheme.markerStroke,
                  weight: activeTheme.markerWeight,
                }}
              >
                <Popup>
                  <div className="text-sm">
                    <div className="font-semibold">{point.label}</div>
                    <div className="text-muted-foreground mt-1">
                      {point.count} {point.count === 1 ? 'click' : 'clicks'}
                    </div>
                    <div className="text-xs text-muted-foreground mt-1">
                      {point.latitude.toFixed(4)}°, {point.longitude.toFixed(4)}°
                    </div>
                  </div>
                </Popup>
              </CircleMarker>
            );
          })}
        </MapContainer>
      </div>

      <div className="mt-3 flex items-center justify-center gap-4 text-xs text-muted-foreground">
        <div className="flex items-center gap-2">
          <div className={`w-2 h-2 rounded-full ${activeTheme.legendBg} ${activeTheme.legendBorder} border`} />
          <span>Smaller = Fewer clicks</span>
        </div>
        <div className="flex items-center gap-2">
          <div className={`w-4 h-4 rounded-full ${activeTheme.legendBg} ${activeTheme.legendBorder} border`} />
          <span>Larger = More clicks</span>
        </div>
        <div className="text-muted-foreground">
          • Click circles for details
        </div>
      </div>
    </div>
  );
}
