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

export interface Tour {
  id?: number;
  name: string;
  description: string;
  waypoints: Waypoint[]; // From und To mit Zwischenschritten
  transportType: 'car' | 'bike' | 'walking';

  // Felder von API befüllt
  distance?: number;
  estimatedTime?: number;

  // Hier speicherst du die kompletten GeoJSON-Daten für Leaflet
  routeInformation: RouteMetadata;

  // Statistiken (werden später vom Backend berechnet)
  popularity?: number;
  childFriendliness?: number;
}
