import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface SalonClosure {
  id?: string;
  date: string;
  reason?: string;
}

@Injectable({ providedIn: 'root' })
export class SalonClosureService {
  private readonly apiUrl = `${environment.apiUrl}/tenants/closures`;

  constructor(private http: HttpClient) {}

  getAll(): Observable<any> {
    return this.http.get(this.apiUrl);
  }

  create(data: { date: string; reason?: string }): Observable<any> {
    return this.http.post(this.apiUrl, data);
  }

  delete(id: string): Observable<any> {
    return this.http.delete(`${this.apiUrl}/${id}`);
  }
}
