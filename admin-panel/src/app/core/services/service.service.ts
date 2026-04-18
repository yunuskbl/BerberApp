import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Service, CreateServiceRequest, UpdateServiceRequest } from '../models/service.model';
import { ApiResponse } from '../models/api-response.model';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class ServiceService {
  private apiUrl = `${environment.apiUrl}/services`;

  constructor(private http: HttpClient) {}

  getAll(): Observable<ApiResponse<Service[]>> {
    return this.http.get<ApiResponse<Service[]>>(this.apiUrl);
  }

  getById(id: string): Observable<ApiResponse<Service>> {
    return this.http.get<ApiResponse<Service>>(`${this.apiUrl}/${id}`);
  }

  create(request: CreateServiceRequest): Observable<ApiResponse<Service>> {
    return this.http.post<ApiResponse<Service>>(this.apiUrl, request);
  }

  update(id: string, request: UpdateServiceRequest): Observable<ApiResponse<Service>> {
    return this.http.put<ApiResponse<Service>>(`${this.apiUrl}/${id}`, request);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}