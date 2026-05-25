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
  isDeleted: boolean;
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

export interface PaymentMethod {
  id: string;
  name: string;
  bankName: string;
  iban: string;
  accountHolder: string;
  description?: string;
  isActive: boolean;
  order: number;
  createdAt: string;
}

export interface ContactMessage {
  id: string;
  tenantId: string;
  tenantName: string;
  senderEmail: string;
  subject: string;
  message: string;
  status: 'New' | 'Read' | 'Replied';
  reply?: string;
  repliedAt?: string;
  createdAt: string;
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

export interface RevenueReport {
  totalRevenue: number;
  totalRefunded: number;
  netRevenue: number;
  monthlyRevenue: { year: number; month: number; total: number; count: number }[];
  byPlan: { plan: string; total: number; count: number }[];
}

export interface AppointmentStats {
  monthly: { year: number; month: number; total: number; completed: number; cancelled: number; pending: number }[];
  topServices: { service: string; count: number }[];
}

@Injectable({ providedIn: 'root' })
export class SuperAdminService {
  private apiUrl = `${environment.apiUrl}/superadmin`;

  constructor(private http: HttpClient) {}

  getAllTenants(includeDeleted = false): Observable<ApiResponse<SuperAdminTenant[]>> {
    const params = includeDeleted ? new HttpParams().set('includeDeleted', 'true') : new HttpParams();
    return this.http.get<ApiResponse<SuperAdminTenant[]>>(`${this.apiUrl}/tenants`, { params });
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

  resetAppointments(tenantId: string): Observable<ApiResponse<any>> {
    return this.http.post<ApiResponse<any>>(`${this.apiUrl}/tenants/${tenantId}/reset-appointments`, {});
  }

  hardDeleteTenant(tenantId: string): Observable<ApiResponse<any>> {
    return this.http.delete<ApiResponse<any>>(`${this.apiUrl}/tenants/${tenantId}/permanent`);
  }

  resetTenantData(tenantId: string): Observable<ApiResponse<any>> {
    return this.http.post<ApiResponse<any>>(`${this.apiUrl}/tenants/${tenantId}/reset`, {});
  }

  restoreTenant(tenantId: string): Observable<ApiResponse<any>> {
    return this.http.post<ApiResponse<any>>(`${this.apiUrl}/tenants/${tenantId}/restore`, {});
  }

  getReports(): Observable<ApiResponse<SuperAdminReports>> {
    return this.http.get<ApiResponse<SuperAdminReports>>(`${this.apiUrl}/reports`);
  }

  getSubscriptions(status?: string): Observable<ApiResponse<SuperAdminSubscription[]>> {
    let params = new HttpParams();
    if (status) params = params.set('status', status);
    return this.http.get<ApiResponse<SuperAdminSubscription[]>>(`${this.apiUrl}/subscriptions`, { params });
  }

  // Ödeme Yöntemleri
  getPaymentMethods(): Observable<ApiResponse<PaymentMethod[]>> {
    return this.http.get<ApiResponse<PaymentMethod[]>>(`${this.apiUrl}/payment-methods`);
  }

  createPaymentMethod(req: Omit<PaymentMethod, 'id' | 'createdAt'>): Observable<ApiResponse<PaymentMethod>> {
    return this.http.post<ApiResponse<PaymentMethod>>(`${this.apiUrl}/payment-methods`, req);
  }

  updatePaymentMethod(id: string, req: Omit<PaymentMethod, 'id' | 'createdAt'>): Observable<ApiResponse<any>> {
    return this.http.put<ApiResponse<any>>(`${this.apiUrl}/payment-methods/${id}`, req);
  }

  deletePaymentMethod(id: string): Observable<ApiResponse<any>> {
    return this.http.delete<ApiResponse<any>>(`${this.apiUrl}/payment-methods/${id}`);
  }

  // İletişim Mesajları
  getContactMessages(status?: string): Observable<ApiResponse<ContactMessage[]>> {
    let params = new HttpParams();
    if (status) params = params.set('status', status);
    return this.http.get<ApiResponse<ContactMessage[]>>(`${this.apiUrl}/contact-messages`, { params });
  }

  replyToMessage(id: string, reply: string): Observable<ApiResponse<any>> {
    return this.http.post<ApiResponse<any>>(`${this.apiUrl}/contact-messages/${id}/reply`, { reply });
  }

  markMessageRead(id: string): Observable<ApiResponse<any>> {
    return this.http.patch<ApiResponse<any>>(`${this.apiUrl}/contact-messages/${id}/read`, {});
  }

  deleteContactMessage(id: string): Observable<ApiResponse<any>> {
    return this.http.delete<ApiResponse<any>>(`${this.apiUrl}/contact-messages/${id}`);
  }

  updateTenant(id: string, req: { name?: string; phone?: string; address?: string; isActive?: boolean }): Observable<ApiResponse<any>> {
    return this.http.put<ApiResponse<any>>(`${this.apiUrl}/tenants/${id}`, req);
  }

  extendSubscription(id: string, days: number): Observable<ApiResponse<any>> {
    return this.http.patch<ApiResponse<any>>(`${this.apiUrl}/tenants/${id}/extend`, { days });
  }

  resetAdminPassword(id: string, newPassword: string): Observable<ApiResponse<any>> {
    return this.http.post<ApiResponse<any>>(`${this.apiUrl}/tenants/${id}/reset-password`, { newPassword });
  }

  notifyTenant(id: string, message: string): Observable<ApiResponse<any>> {
    return this.http.post<ApiResponse<any>>(`${this.apiUrl}/tenants/${id}/notify`, { message });
  }

  broadcast(message: string): Observable<ApiResponse<any>> {
    return this.http.post<ApiResponse<any>>(`${this.apiUrl}/broadcast`, { message });
  }

  getRevenue(months = 6): Observable<ApiResponse<RevenueReport>> {
    const params = new HttpParams().set('months', months.toString());
    return this.http.get<ApiResponse<RevenueReport>>(`${this.apiUrl}/revenue`, { params });
  }

  getTenantAppointmentStats(id: string, months = 6): Observable<ApiResponse<AppointmentStats>> {
    const params = new HttpParams().set('months', months.toString());
    return this.http.get<ApiResponse<AppointmentStats>>(`${this.apiUrl}/tenants/${id}/appointment-stats`, { params });
  }
}
