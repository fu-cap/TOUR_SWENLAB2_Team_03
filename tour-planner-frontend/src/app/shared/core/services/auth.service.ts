import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';

export interface RegisterRequest {
  username: string;
  email: string;
  password: string;
  gender: string;
  firstname: string;
  lastname: string;
}

export interface LoginRequest {
  username: string;
  password: string;
}

export interface UserProfile {
  id: string;
  username: string;
  email: string;
}

export interface UserFullProfile {
  id: string;
  username: string;
  email: string;
  gender: string;
  firstName: string;
  lastName: string;
}

export interface UpdateUserRequest {
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

  // Global state for the authenticated user
  currentUser = signal<UserProfile | null>(null);

  constructor() {
    const savedUser = localStorage.getItem('currentUser');
    if (savedUser) {
      try {
        this.currentUser.set(JSON.parse(savedUser));
      } catch (e) {
        localStorage.removeItem('currentUser');
      }
    }
  }

  register(request: RegisterRequest): Observable<any> {
    return this.http.post<any>(this.API_URL, request);
  }

  login(request: LoginRequest): Observable<UserProfile> {
    return this.http.post<UserProfile>(`${this.API_URL}/login`, request).pipe(
      tap(user => {
        this.currentUser.set(user);
        localStorage.setItem('currentUser', JSON.stringify(user));
      })
    );
  }

  logout() {
    this.currentUser.set(null);
    localStorage.removeItem('currentUser');
  }

  getUserById(id: string): Observable<UserFullProfile> {
    return this.http.get<UserFullProfile>(`${this.API_URL}/${id}`);
  }

  updateUser(id: string, data: UpdateUserRequest): Observable<void> {
    return this.http.put<void>(`${this.API_URL}/${id}`, data).pipe(
      tap(() => {
        const updated: UserProfile = { id, username: data.username, email: data.email };
        this.currentUser.set(updated);
        localStorage.setItem('currentUser', JSON.stringify(updated));
      })
    );
  }

  deleteUser(id: string): Observable<void> {
    return this.http.delete<void>(`${this.API_URL}/${id}`);
  }
}
