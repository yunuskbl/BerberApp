import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface ExpenseDto {
  id:           string;
  date:         string;
  amount:       number;
  currency:     string;
  category:     string;
  description?: string;
  note?:        string;
  createdAt:    string;
}

export interface CreateExpenseRequest {
  date:         string;
  amount:       number;
  currency:     string;
  category:     string;
  description?: string;
  note?:        string;
}

@Injectable({ providedIn: 'root' })
export class ExpenseService {
  private apiUrl = `${environment.apiUrl}/expenses`;

  constructor(private http: HttpClient) {}

  getExpenses(startDate?: string, endDate?: string): Observable<any> {
    let url = this.apiUrl;
    const params: string[] = [];
    if (startDate) params.push(`startDate=${startDate}`);
    if (endDate)   params.push(`endDate=${endDate}`);
    if (params.length) url += '?' + params.join('&');
    return this.http.get(url);
  }

  createExpense(req: CreateExpenseRequest): Observable<any> {
    return this.http.post(this.apiUrl, req);
  }

  updateExpense(id: string, req: CreateExpenseRequest): Observable<any> {
    return this.http.put(`${this.apiUrl}/${id}`, req);
  }

  deleteExpense(id: string): Observable<any> {
    return this.http.delete(`${this.apiUrl}/${id}`);
  }
}
