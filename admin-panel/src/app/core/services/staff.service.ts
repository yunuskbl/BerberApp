import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Staff, CreateStaffRequest, UpdateStaffRequest } from '../models/staff.model';
import { ApiResponse } from '../models/api-response.model';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class StaffService {
  private apiUrl = `${environment.apiUrl}/staff`;

  constructor(private http: HttpClient) {}

  getAll(): Observable<ApiResponse<Staff[]>> {
    return this.http.get<ApiResponse<Staff[]>>(this.apiUrl);
  }

  getById(id: string): Observable<ApiResponse<Staff>> {
    return this.http.get<ApiResponse<Staff>>(`${this.apiUrl}/${id}`);
  }

  create(request: CreateStaffRequest): Observable<ApiResponse<Staff>> {
    return this.http.post<ApiResponse<Staff>>(this.apiUrl, request);
  }

  update(id: string, request: UpdateStaffRequest): Observable<ApiResponse<Staff>> {
    return this.http.put<ApiResponse<Staff>>(`${this.apiUrl}/${id}`, request);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}