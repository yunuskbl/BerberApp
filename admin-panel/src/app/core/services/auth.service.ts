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
          localStorage.setItem('isOnTrial', response.data.isOnTrial ? 'true' : 'false');
          localStorage.setItem('trialEndsAt', response.data.trialEndsAt ?? '');
          localStorage.setItem('subscriptionExpired', response.data.subscriptionExpired ? 'true' : 'false');
          localStorage.setItem('isEmailVerified', response.data.isEmailVerified !== false ? 'true' : 'false');

          try {
            const decoded: any = jwtDecode(response.data.accessToken);
            localStorage.setItem('userPlan', decoded.plan_type || 'Baslangic');
          } catch {
            localStorage.setItem('userPlan', 'Baslangic');
          }
        }
      })
    );
  }

  register(request: any): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/register`, request).pipe(
      tap(response => {
        if (response.success) {
          localStorage.setItem('accessToken', response.data.accessToken);
          localStorage.setItem('refreshToken', response.data.refreshToken);
          localStorage.setItem('user', JSON.stringify(response.data));
          localStorage.setItem('subdomain', response.data.subdomain || '');
          localStorage.setItem('isOnTrial', response.data.isOnTrial ? 'true' : 'false');
          localStorage.setItem('trialEndsAt', response.data.trialEndsAt ?? '');
          localStorage.setItem('subscriptionExpired', response.data.subscriptionExpired ? 'true' : 'false');
          localStorage.setItem('isEmailVerified', response.data.isEmailVerified ? 'true' : 'false');
          try {
            const decoded: any = jwtDecode(response.data.accessToken);
            localStorage.setItem('userPlan', decoded.plan_type || 'Baslangic');
          } catch {
            localStorage.setItem('userPlan', 'Baslangic');
          }
        }
      })
    );
  }

  sendRegistrationOtp(phone: string): Observable<any> {
    return this.http.post<any>(`${environment.apiUrl}/otp/send`, { phone });
  }

  verifyRegistrationOtp(phone: string, code: string): Observable<any> {
    return this.http.post<any>(`${environment.apiUrl}/otp/verify`, { phone, code });
  }

  isEmailVerified(): boolean {
    return localStorage.getItem('isEmailVerified') !== 'false';
  }

  setEmailVerified(): void {
    localStorage.setItem('isEmailVerified', 'true');
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
          // Yeni token'dan planı decode edip güncelle
          try {
            const decoded: any = jwtDecode(response.data.accessToken);
            if (decoded.plan_type) {
              localStorage.setItem('userPlan', decoded.plan_type);
            }
          } catch { /* ignore */ }
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
    localStorage.removeItem('isOnTrial');
    localStorage.removeItem('trialEndsAt');
    localStorage.removeItem('subscriptionExpired');
  }

  isOnTrial(): boolean {
    return localStorage.getItem('isOnTrial') === 'true';
  }

  getTrialEndsAt(): Date | null {
    const val = localStorage.getItem('trialEndsAt');
    return val ? new Date(val) : null;
  }

  getTrialDaysLeft(): number {
    const end = this.getTrialEndsAt();
    if (!end) return 0;
    const diff = end.getTime() - Date.now();
    return Math.max(0, Math.ceil(diff / (1000 * 60 * 60 * 24)));
  }

  isSubscriptionExpired(): boolean {
    return localStorage.getItem('subscriptionExpired') === 'true';
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
    const plan = localStorage.getItem('userPlan') || 'Baslangic';
    // Eski token'lar için geriye dönük uyumluluk
    const compat: Record<string, string> = { Basic: 'Baslangic', Standard: 'Profesyonel', Full: 'Premium' };
    return compat[plan] ?? plan;
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

  isStaff(): boolean {
    return this.getUserRole() === 'Staff';
  }

  getStaffId(): string | null {
    try {
      const token = this.getToken();
      if (!token) return null;
      const decoded: any = jwtDecode(token);
      return decoded.staff_id ?? null;
    } catch {
      return null;
    }
  }
}
