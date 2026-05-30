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
  tour_id: string;
  date_time: string;
  comment: string;
  difficulty: number;
  total_distance_km: number;
  total_time_min: string; // TimeSpan string
  rating: number;
}

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
