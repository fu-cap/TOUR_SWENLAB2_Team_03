import { Injectable, inject, signal } from '@angular/core';
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
  private readonly API_URL = 'http://localhost:8080/api/tour'; // Adjust based on backend

  // Global state for sharing between list and details/edit
  selectedTour = signal<Tour | null>(null);

  createTour(tourData: CreateTourRequest): Observable<Tour> {
    return this.http.post<Tour>(this.API_URL, tourData);
  }

  getTours(userId: string, search?: string): Observable<Tour[]> {
    const params: any = { userId: userId };
    if (search) {
      params.search = search;
    }
    return this.http.get<Tour[]>(this.API_URL, { params });
  }

  getTourById(id: string): Observable<Tour> {
    return this.http.get<Tour>(`${this.API_URL}/${id}`);
  }

  deleteTour(id: string): Observable<void> {
    return this.http.delete<void>(`${this.API_URL}/${id}`);
  }

  updateTour(id: string, tourData: CreateTourRequest): Observable<void> {
    return this.http.put<void>(`${this.API_URL}/${id}`, tourData);
  }

  exportTours(userId: string): Observable<any[]> {
    return this.http.get<any[]>(`${this.API_URL}/export?userId=${userId}`);
  }

  importTours(userId: string, tours: any[]): Observable<any> {
    return this.http.post<any>(`${this.API_URL}/import?userId=${userId}`, tours);
  }
}
