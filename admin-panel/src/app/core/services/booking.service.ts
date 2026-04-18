import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface SalonInfo {
  id:       string;
  name:     string;
  phone?:   string;
  address?: string;
  logoUrl?: string;
}

export interface BookingService {
  id:              string;
  name:            string;
  durationMinutes: number;
  price:           number;
  currency:        string;
  color?:          string;
}

export interface BookingStaff {
  id:        string;
  fullName:  string;
  avatarUrl?: string;
  bio?:      string;
}

export interface BookingSlot {
  startTime:   string;
  endTime:     string;
  isAvailable: boolean;
}

export interface BookingRequest {
  fullName:  string;
  phone:     string;
  email?:    string;
  staffId:   string;
  serviceId: string;
  startTime: string;
  notes?:    string;
}

@Injectable({ providedIn: 'root' })
export class BookingApiService {
  private apiUrl = environment.apiUrl;

  constructor(private http: HttpClient) {}

  getSalon(subdomain: string): Observable<any> {
    return this.http.get(`${this.apiUrl}/booking/${subdomain}`);
  }

  getServices(subdomain: string): Observable<any> {
    return this.http.get(`${this.apiUrl}/booking/${subdomain}/services`);
  }

  getStaff(subdomain: string): Observable<any> {
    return this.http.get(`${this.apiUrl}/booking/${subdomain}/staff`);
  }

  getAvailableSlots(subdomain: string, staffId: string, serviceId: string, date: string): Observable<any> {
    const params = new HttpParams()
      .set('staffId',   staffId)
      .set('serviceId', serviceId)
      .set('date',      date);
    return this.http.get(`${this.apiUrl}/booking/${subdomain}/available-slots`, { params });
  }

  createAppointment(subdomain: string, request: BookingRequest): Observable<any> {
    return this.http.post(`${this.apiUrl}/booking/${subdomain}/appointments`, request);
  }
}