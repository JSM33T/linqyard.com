"use client";

import { useEffect, useMemo } from 'react';
import dynamic from 'next/dynamic';
import type { LatLngExpression } from 'leaflet';
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

const CircleMarker = dynamic(
  () => import('react-leaflet').then((mod) => mod.CircleMarker),
  { ssr: false }
);

const Popup = dynamic(
  () => import('react-leaflet').then((mod) => mod.Popup),
  { ssr: false }
);

interface WorldMapProps {
  data: Array<{
    country: string;
    city?: string | null;
    count: number;
    latitude?: number | null;
    longitude?: number | null;
  }>;
  height?: number;
}

export function WorldMap({ data, height = 400 }: WorldMapProps) {
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
          <TileLayer
            attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>'
            url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
          />
          
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
                  fillColor: '#3b82f6',
                  fillOpacity: 0.6,
                  color: '#1e40af',
                  weight: 2,
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
          <div className="w-2 h-2 rounded-full bg-blue-500 border border-blue-700" />
          <span>Smaller = Fewer clicks</span>
        </div>
        <div className="flex items-center gap-2">
          <div className="w-4 h-4 rounded-full bg-blue-500 border border-blue-700" />
          <span>Larger = More clicks</span>
        </div>
        <div className="text-muted-foreground">
          • Click circles for details
        </div>
      </div>
    </div>
  );
}
