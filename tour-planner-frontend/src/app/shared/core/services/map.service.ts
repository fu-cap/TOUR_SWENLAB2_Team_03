import { Injectable, signal } from '@angular/core';
import { Subject } from 'rxjs';

export interface MapMarker {
  id: number;
  lat: number;
  lng: number;
  label?: string;
}

@Injectable({
  providedIn: 'root'
})
export class MapService {
  // Use a Subject to send commands to the map component
  private markerUpdateSource = new Subject<MapMarker[]>();
  markerUpdates$ = this.markerUpdateSource.asObservable();

  private routeUpdateSource = new Subject<string>(); // Encoded geometry or GeoJSON
  routeUpdates$ = this.routeUpdateSource.asObservable();

  private clearMapSource = new Subject<void>();
  clearMap$ = this.clearMapSource.asObservable();

  updateMarkers(markers: MapMarker[]) {
    this.markerUpdateSource.next(markers);
  }

  updateRoute(geometry: string) {
    this.routeUpdateSource.next(geometry);
  }

  clearMap() {
    this.clearMapSource.next();
  }
}
