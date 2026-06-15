export interface Waypoint {
  id?: string;
  tourId?: string;
  orderIndex?: number;
  label: string;
  latitude: number;
  longitude: number;
}

export type TransportType = 
  | 'driving-car' 
  | 'driving-hgv' 
  | 'cycling-regular' 
  | 'cycling-road' 
  | 'foot-walking' 
  | 'foot-hiking';

export interface TourLog {
  id?: string;
  tourId: string;
  dateTime: string;
  comment: string;
  difficulty: number;
  totalDistanceKm: number;
  totalTimeMin: string; // TimeSpan string
  rating: number;
  createdAt?: string;
  updatedAt?: string;
}

export interface Tour {
  id?: string; // Guid is string in JSON
  name: string;
  description: string;
  waypoints: Waypoint[]; 
  transportType: TransportType;

  // Felder von API befüllt
  distanceKm?: number;
  estimatedTime?: string; // TimeSpan string from Backend "HH:mm:ss"

  // Hier speicherst du die kompletten GeoJSON-Daten für Leaflet
  routeInformation: string; // Backend sends polyline string

  // Statistiken
  popularity?: number;
  childFriendliness?: number;
}
