import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface SalonPhoto {
  id: string;
  url: string;
}

export interface SalonInfo {
  id: string;
  name: string;
  phone?: string;
  address?: string;
  logoUrl?: string;
  themeColor?: string;
  photos?: SalonPhoto[];
  averageRating?: number;
  totalReviews?: number;
  isAtCapacity?: boolean;
  isNearCapacity?: boolean;
}

export interface BookingService {
  id: string;
  name: string;
  nameEn?: string;
  nameRu?: string;
  durationMinutes: number;
  price: number;
  currency: string;
  color?: string;
  minPrice: number | null;
  maxPrice: number | null;
  hasMixedCurrencies: boolean;
}

export interface BookingStaff {
  id: string;
  fullName: string;
  avatarUrl?: string;
  bio?: string;
  price?: number;
  currency?: string;
  hasCustomPrice?: boolean;
}

export interface BookingSlot {
  startTime: string;
  endTime: string;
  isAvailable: boolean;
}

export interface BookingRequest {
  fullName: string;
  phone: string;
  email?: string;
  staffId: string;
  serviceId: string;
  serviceIds?: string[];
  startTime: string;
  notes?: string;
}

@Injectable({ providedIn: 'root' })
export class BookingApiService {
  private apiUrl = environment.apiUrl;

  constructor(private http: HttpClient) {}

  getSalon(subdomain: string): Observable<any> {
    return this.http.get(`${this.apiUrl}/booking/${subdomain}`);
  }

  getServices(subdomain: string, staffId?: string): Observable<any> {
    const params = staffId ? new HttpParams().set('staffId', staffId) : undefined;
    return this.http.get(`${this.apiUrl}/booking/${subdomain}/services`, { params });
  }

  getStaff(subdomain: string): Observable<any> {
    return this.http.get(`${this.apiUrl}/booking/${subdomain}/staff`);
  }

  getAvailableSlots(
    subdomain: string,
    staffId: string,
    serviceIds: string[],
    date: string,
  ): Observable<any> {
    let params = new HttpParams()
      .set('staffId', staffId)
      .set('serviceId', serviceIds[0] ?? '')
      .set('date', date);
    serviceIds.forEach(id => { params = params.append('serviceIds', id); });
    return this.http.get(
      `${this.apiUrl}/booking/${subdomain}/available-slots`,
      { params },
    );
  }

  createAppointment(
    subdomain: string,
    request: BookingRequest,
  ): Observable<any> {
    return this.http.post(
      `${this.apiUrl}/booking/${subdomain}/appointments`,
      request,
    );
  }
  getAppointmentStatus(subdomain: string, appointmentId: string, phone: string): Observable<any> {
    const params = new HttpParams().set('phone', phone);
    return this.http.get(
      `${this.apiUrl}/booking/${subdomain}/appointments/${appointmentId}`,
      { params }
    );
  }

  cancelAppointment(subdomain: string, appointmentId: string, phone: string): Observable<any> {
    const params = new HttpParams().set('phone', phone);
    return this.http.post(
      `${this.apiUrl}/booking/${subdomain}/appointments/${appointmentId}/cancel`,
      {},
      { params }
    );
  }

  lookupCustomer(subdomain: string, phone: string): Observable<any> {
    const params = new HttpParams().set('phone', phone);
    return this.http.get(`${this.apiUrl}/booking/${subdomain}/customer-lookup`, { params });
  }

  getMyAppointments(subdomain: string, phone: string): Observable<any> {
    const params = new HttpParams().set('phone', phone);
    return this.http.get(`${this.apiUrl}/booking/${subdomain}/my-appointments`, { params });
  }

  sendOtp(phone: string, email?: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/otp/send`, { phone, email: email || null });
  }

  verifyOtp(phone: string, code: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/otp/verify`, { phone, code });
  }
}
