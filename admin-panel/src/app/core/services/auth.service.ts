import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { LoginRequest, LoginResponse } from '../models/auth.model';
import { ApiResponse } from '../models/api-response.model';
import { environment } from '../../../environments/environment';
import { jwtDecode } from 'jwt-decode';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private apiUrl = `${environment.apiUrl}/auth`;

  constructor(private http: HttpClient) {}

  login(request: LoginRequest): Observable<ApiResponse<LoginResponse>> {
    return this.http.post<ApiResponse<LoginResponse>>(`${this.apiUrl}/login`, request).pipe(
      tap(response => {
        if (response.success) {
          localStorage.setItem('accessToken', response.data.accessToken);
          localStorage.setItem('refreshToken', response.data.refreshToken);
          localStorage.setItem('user', JSON.stringify(response.data));
          localStorage.setItem('subdomain', response.data.subdomain || '');

          // 🔑 Decode JWT ve plan'ı al
          try {
            const decoded: any = jwtDecode(response.data.accessToken);
            const userPlan = decoded.plan_type || 'Basic';
            localStorage.setItem('userPlan', userPlan);
          } catch (error) {
            console.error('Token decode error:', error);
            localStorage.setItem('userPlan', 'Basic');
          }
        }
      })
    );
  }

  logout(): void {
    localStorage.removeItem('accessToken');
    localStorage.removeItem('refreshToken');
    localStorage.removeItem('user');
    localStorage.removeItem('userPlan');
  }

  getToken(): string | null {
    return localStorage.getItem('accessToken');
  }

  getUser(): LoginResponse | null {
    const user = localStorage.getItem('user');
    return user ? JSON.parse(user) : null;
  }

  getUserPlan(): string {
    return localStorage.getItem('userPlan') || 'Basic';
  }

  isLoggedIn(): boolean {
    return !!this.getToken();
  }

  getUserRole(): string {
    const user = this.getUser();
    return user?.role || '';
  }

  isSuperAdmin(): boolean {
    return this.getUserRole() === 'SuperAdmin';
  }
}