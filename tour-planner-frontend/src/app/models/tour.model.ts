export interface RouteMetadata {
  distance: number;       // Gesamtlänge in Metern
  duration: number;       // Gesamtzeit in Sekunden
  geometry: number[][];   // Alle Punkte des Pfads für die Linie
  waypointCoords: number[][]; // Nur die Koordinaten deiner tatsächlichen Stopps für die Marker[cite: 1]
}

export interface Waypoint {
  lat : number;
  lng: number;
  adresse?: string;
}

export type TransportType = 
  | 'driving-car' 
  | 'driving-hgv' 
  | 'cycling-regular' 
  | 'cycling-road' 
  | 'foot-walking' 
  | 'foot-hiking';

export interface Tour {
  id?: string; // Guid is string in JSON
  name: string;
  description: string;
  waypoints: Waypoint[]; 
  transportType: TransportType;

  // Felder von API befüllt
  distance_km?: number;
  estimatedTime?: string; // TimeSpan string from Backend "HH:mm:ss"

  // Hier speicherst du die kompletten GeoJSON-Daten für Leaflet
  routeInformation: string; // Backend sends polyline string

  // Statistiken
  popularity?: number;
  childFriendliness?: number;
}
