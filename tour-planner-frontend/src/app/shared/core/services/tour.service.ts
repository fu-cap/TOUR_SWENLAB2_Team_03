import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Tour, TransportType } from '@/models/tour.model';

export interface CreateTourRequest {
  userId: string; // Guid
  name: string;
  description: string;
  waypoints: {
    label: string;
    latitude: number;
    longitude: number;
  }[];
  transportType: TransportType;
}

@Injectable({
  providedIn: 'root'
})
export class TourService {
  private http = inject(HttpClient);
  private readonly API_URL = 'http://localhost:8080/api/Tour'; // Adjust based on backend

  createTour(tourData: CreateTourRequest): Observable<Tour> {
    return this.http.post<Tour>(this.API_URL, tourData);
  }

  getTours(): Observable<Tour[]> {
    return this.http.get<Tour[]>(this.API_URL);
  }

  getTourById(id: number): Observable<Tour> {
    return this.http.get<Tour>(`${this.API_URL}/${id}`);
  }
}
