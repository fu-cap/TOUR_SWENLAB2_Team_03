import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { TourLog } from '@/models/tour.model';

@Injectable({
  providedIn: 'root'
})
export class LogService {
  private http = inject(HttpClient);
  private readonly API_URL = 'http://localhost:8080/api/log';

  getLogsByTourId(tourId: string): Observable<TourLog[]> {
    return this.http.get<TourLog[]>(`${this.API_URL}/${tourId}`);
  }

  createLog(log: TourLog): Observable<void> {
    return this.http.post<void>(this.API_URL, log);
  }

  getAllLogs(): Observable<TourLog[]> {
    return this.http.get<TourLog[]>(this.API_URL);
  }
}
