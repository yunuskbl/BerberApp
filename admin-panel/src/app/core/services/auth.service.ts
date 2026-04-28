import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap, catchError, of } from 'rxjs';
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

          try {
            const decoded: any = jwtDecode(response.data.accessToken);
            localStorage.setItem('userPlan', decoded.plan_type || 'Basic');
          } catch {
            localStorage.setItem('userPlan', 'Basic');
          }
        }
      })
    );
  }

  logout(): void {
    // Sunucuda refresh token'ı geçersiz kıl (hata olursa yine de local temizle)
    this.http.post(`${this.apiUrl}/logout`, {}).pipe(
      catchError(() => of(null))
    ).subscribe();

    this.clearLocalStorage();
  }

  refreshToken(): Observable<any> {
    const refreshToken = localStorage.getItem('refreshToken');
    return this.http.post<any>(`${this.apiUrl}/refresh`, { refreshToken }).pipe(
      tap(response => {
        if (response.success) {
          localStorage.setItem('accessToken', response.data.accessToken);
          localStorage.setItem('refreshToken', response.data.refreshToken);
        }
      })
    );
  }

  private clearLocalStorage(): void {
    localStorage.removeItem('accessToken');
    localStorage.removeItem('refreshToken');
    localStorage.removeItem('user');
    localStorage.removeItem('userPlan');
    localStorage.removeItem('subdomain');
  }

  getToken(): string | null {
    return localStorage.getItem('accessToken');
  }

  getRefreshToken(): string | null {
    return localStorage.getItem('refreshToken');
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
    return this.getUser()?.role || '';
  }

  isSuperAdmin(): boolean {
    return this.getUserRole() === 'SuperAdmin';
  }
}
