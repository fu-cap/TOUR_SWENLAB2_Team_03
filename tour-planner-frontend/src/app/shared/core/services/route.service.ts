import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { TransportType } from '@/models/tour.model';

@Injectable({
  providedIn: 'root'
})
export class RouteService {
  private http = inject(HttpClient);
  private readonly PROXY_URL = 'http://localhost:8080/api/map/directions';

  getRoute(coordinates: number[][], transportType: TransportType): Observable<any> {
    return this.http.post<any>(`${this.PROXY_URL}/${transportType}`, coordinates);
  }
}
