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
  totalTimeMin: string;
  rating: number;
  createdAt?: string;
  updatedAt?: string;
}

export interface Tour {
  id?: string;
  name: string;
  description: string;
  waypoints: Waypoint[];
  transportType: TransportType;
  distanceKm?: number;
  estimatedTime?: string;
  routeInformation: string;
  popularity?: number;
  childFriendliness?: number;
  co2EmittedGrams?: number;
  co2SavedGrams?: number;
}
