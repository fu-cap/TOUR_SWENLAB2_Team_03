import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface RegisterRequest {
  username: string;
  email: string;
  password: string;
  gender: string;
  firstname: string;
  lastname: string;
}

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private http = inject(HttpClient);
  private readonly API_URL = 'http://localhost:8080/api/user';

  register(request: RegisterRequest): Observable<any> {
    return this.http.post<any>(this.API_URL, request);
  }
}
