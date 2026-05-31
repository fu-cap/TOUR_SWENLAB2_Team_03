import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map, of } from 'rxjs';

export interface GeocodingResult {
  address: string;
  lat: number;
  lng: number;
}

@Injectable({
  providedIn: 'root'
})
export class GeocodingService {
  private http = inject(HttpClient);
  // Now calling our own backend proxy to keep the API key safe
  private readonly PROXY_URL = 'http://localhost:8080/api/map/geocode';

  search(text: string): Observable<GeocodingResult[]> {
    if (!text || text.length < 3) return of([]);

    return this.http.get<any>(this.PROXY_URL, {
      params: { text: text }
    }).pipe(
      map(response => {
        return response.features.map((f: any) => ({
          address: f.properties.label,
          lng: f.geometry.coordinates[0],
          lat: f.geometry.coordinates[1]
        }));
      })
    );
  }
}
