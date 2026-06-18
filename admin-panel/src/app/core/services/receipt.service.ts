import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Receipt, CreateReceiptDto } from '../models/receipt.model';
import { ApiResponse } from '../models/api-response.model';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class ReceiptService {
  private apiUrl = `${environment.apiUrl}/receipts`;

  constructor(private http: HttpClient) {}

  getAll(filters?: { from?: string; to?: string; customerId?: string }): Observable<ApiResponse<Receipt[]>> {
    let params = new HttpParams();
    if (filters?.from)       params = params.set('from', filters.from);
    if (filters?.to)         params = params.set('to', filters.to);
    if (filters?.customerId) params = params.set('customerId', filters.customerId);
    return this.http.get<ApiResponse<Receipt[]>>(this.apiUrl, { params });
  }

  getById(id: string): Observable<ApiResponse<Receipt>> {
    return this.http.get<ApiResponse<Receipt>>(`${this.apiUrl}/${id}`);
  }

  create(dto: CreateReceiptDto): Observable<ApiResponse<Receipt>> {
    return this.http.post<ApiResponse<Receipt>>(this.apiUrl, dto);
  }

  void(id: string): Observable<ApiResponse<null>> {
    return this.http.delete<ApiResponse<null>>(`${this.apiUrl}/${id}`);
  }
}
