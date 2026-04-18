import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  Appointment,
  AvailableSlot,
  CreateAppointmentRequest
} from '../models/appointment.model';
import { ApiResponse } from '../models/api-response.model';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class AppointmentService {
  private apiUrl = `${environment.apiUrl}/appointments`;

  constructor(private http: HttpClient) {}

  getAll(staffId?: string, date?: string): Observable<ApiResponse<Appointment[]>> {
    let params = new HttpParams();
    if (staffId) params = params.set('staffId', staffId);
    if (date)    params = params.set('date', date);
    return this.http.get<ApiResponse<Appointment[]>>(this.apiUrl, { params });
  }

  getById(id: string): Observable<ApiResponse<Appointment>> {
    return this.http.get<ApiResponse<Appointment>>(`${this.apiUrl}/${id}`);
  }

  getAvailableSlots(staffId: string, serviceId: string, date: string): Observable<ApiResponse<AvailableSlot[]>> {
    const params = new HttpParams()
      .set('staffId',   staffId)
      .set('serviceId', serviceId)
      .set('date',      date);
    return this.http.get<ApiResponse<AvailableSlot[]>>(`${this.apiUrl}/available-slots`, { params });
  }

  create(request: CreateAppointmentRequest): Observable<ApiResponse<Appointment>> {
    return this.http.post<ApiResponse<Appointment>>(this.apiUrl, request);
  }

  cancel(id: string): Observable<void> {
    return this.http.patch<void>(`${this.apiUrl}/${id}/cancel`, {});
  }

  complete(id: string): Observable<void> {
    return this.http.patch<void>(`${this.apiUrl}/${id}/complete`, {});
  }
}