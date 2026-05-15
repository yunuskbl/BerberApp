import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../models/api-response.model';

export interface SuperAdminTenant {
  id: string;
  name: string;
  subdomain: string;
  logoUrl?: string;
  phone?: string;
  address?: string;
  isActive: boolean;
  createdAt: string;
  adminEmail?: string;
  adminName?: string;
  staffCount: number;
  customerCount: number;
  totalAppointments: number;
  pendingAppointments: number;
  completedAppointments: number;
  plan?: string;
  subscriptionStatus?: string;
  subscriptionExpiresAt?: string;
  isOnTrial: boolean;
  daysLeft?: number;
}

export interface CreateTenantRequest {
  tenantName: string;
  subdomain: string;
  firstName: string;
  lastName: string;
  email: string;
  password: string;
  phone?: string;
  address?: string;
}

export interface SuperAdminReports {
  totalTenants: number;
  activeTenants: number;
  trialTenants: number;
  activePaidTenants: number;
  newThisMonth: number;
  newThisWeek: number;
  totalAppointments: number;
  appointmentsThisMonth: number;
  expiringSoon: number;
  planDistribution: { plan: string; count: number }[];
  monthlyGrowth: { year: number; month: number; count: number }[];
}

export interface SuperAdminSubscription {
  id: string;
  tenantId: string;
  tenantName?: string;
  adminEmail?: string;
  plan: string;
  status: string;
  startDate: string;
  expiryDate: string;
  price: number;
  currency: string;
  createdAt: string;
  isExpired: boolean;
  daysLeft?: number;
}

@Injectable({ providedIn: 'root' })
export class SuperAdminService {
  private apiUrl = `${environment.apiUrl}/superadmin`;

  constructor(private http: HttpClient) {}

  getAllTenants(): Observable<ApiResponse<SuperAdminTenant[]>> {
    return this.http.get<ApiResponse<SuperAdminTenant[]>>(`${this.apiUrl}/tenants`);
  }

  createTenant(request: CreateTenantRequest): Observable<ApiResponse<any>> {
    return this.http.post<ApiResponse<any>>(`${this.apiUrl}/tenants`, request);
  }

  toggleTenantActive(tenantId: string): Observable<ApiResponse<any>> {
    return this.http.patch<ApiResponse<any>>(`${this.apiUrl}/tenants/${tenantId}/toggle`, {});
  }

  getTenantDetail(tenantId: string): Observable<ApiResponse<any>> {
    return this.http.get<ApiResponse<any>>(`${this.apiUrl}/tenants/${tenantId}`);
  }

  changePlan(tenantId: string, plan: string): Observable<ApiResponse<any>> {
    return this.http.patch<ApiResponse<any>>(`${this.apiUrl}/tenants/${tenantId}/plan`, { plan });
  }

  softDeleteTenant(tenantId: string): Observable<ApiResponse<any>> {
    return this.http.delete<ApiResponse<any>>(`${this.apiUrl}/tenants/${tenantId}`);
  }

  hardDeleteTenant(tenantId: string): Observable<ApiResponse<any>> {
    return this.http.delete<ApiResponse<any>>(`${this.apiUrl}/tenants/${tenantId}/permanent`);
  }

  resetTenantData(tenantId: string): Observable<ApiResponse<any>> {
    return this.http.post<ApiResponse<any>>(`${this.apiUrl}/tenants/${tenantId}/reset`, {});
  }

  getReports(): Observable<ApiResponse<SuperAdminReports>> {
    return this.http.get<ApiResponse<SuperAdminReports>>(`${this.apiUrl}/reports`);
  }

  getSubscriptions(status?: string): Observable<ApiResponse<SuperAdminSubscription[]>> {
    let params = new HttpParams();
    if (status) params = params.set('status', status);
    return this.http.get<ApiResponse<SuperAdminSubscription[]>>(`${this.apiUrl}/subscriptions`, { params });
  }
}
