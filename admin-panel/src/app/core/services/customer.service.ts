import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Customer, CreateCustomerRequest, UpdateCustomerRequest } from '../models/customer.model';
import { ApiResponse } from '../models/api-response.model';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class CustomerService {
  private apiUrl = `${environment.apiUrl}/customers`;

  constructor(private http: HttpClient) {}

  getAll(): Observable<ApiResponse<Customer[]>> {
    return this.http.get<ApiResponse<Customer[]>>(this.apiUrl);
  }

  getById(id: string): Observable<ApiResponse<Customer>> {
    return this.http.get<ApiResponse<Customer>>(`${this.apiUrl}/${id}`);
  }

  create(request: CreateCustomerRequest): Observable<ApiResponse<Customer>> {
    return this.http.post<ApiResponse<Customer>>(this.apiUrl, request);
  }

  update(id: string, request: UpdateCustomerRequest): Observable<ApiResponse<Customer>> {
    return this.http.put<ApiResponse<Customer>>(`${this.apiUrl}/${id}`, request);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  /**
   * Gönderimi başlatır. Mesajlar arka planda gider; bu çağrı yalnızca işin
   * kuyruğa alındığını bildirir ve ilerlemenin sorgulanacağı kimliği döner.
   */
  broadcast(payload: {
    message: string;
    imageUrl?: string;
    filter: string;
    filterDays?: number;
    minAppointments?: number;
  }): Observable<ApiResponse<{ broadcastJobId: string; totalCustomers: number; filteredCount: number }>> {
    return this.http.post<ApiResponse<{ broadcastJobId: string; totalCustomers: number; filteredCount: number }>>(
      `${this.apiUrl}/broadcast`, payload
    );
  }

  getBroadcastStatus(id: string): Observable<ApiResponse<BroadcastStatus>> {
    return this.http.get<ApiResponse<BroadcastStatus>>(`${this.apiUrl}/broadcast/${id}`);
  }

  uploadBroadcastImage(file: File): Observable<ApiResponse<{ url: string }>> {
    const form = new FormData();
    form.append('file', file);
    return this.http.post<ApiResponse<{ url: string }>>(`${this.apiUrl}/broadcast/image`, form);
  }
}

/** Arka planda yürüyen toplu mesajın anlık durumu. */
export interface BroadcastStatus {
  id: string;
  status: 'Queued' | 'Running' | 'Completed' | 'Failed';
  isFinished: boolean;
  error?: string | null;
  total: number;
  sent: number;
  failed: number;
  skipped: number;
  createdAt: string;
  completedAt?: string | null;
  problems: Array<{
    name?: string | null;
    phone?: string | null;
    status: 'Failed' | 'Skipped';
    error?: string | null;
  }>;
}
