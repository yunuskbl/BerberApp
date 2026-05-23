import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface StaffDayOff {
  id?: string;
  staffId: string;
  date: string;
  reason?: string;
}

@Injectable({ providedIn: 'root' })
export class StaffDaysOffService {
  constructor(private http: HttpClient) {}

  getByStaff(staffId: string): Observable<any> {
    return this.http.get(`${environment.apiUrl}/staff/${staffId}/days-off`);
  }

  create(staffId: string, data: { date: string; reason?: string }): Observable<any> {
    return this.http.post(`${environment.apiUrl}/staff/${staffId}/days-off`, data);
  }

  delete(staffId: string, id: string): Observable<any> {
    return this.http.delete(`${environment.apiUrl}/staff/${staffId}/days-off/${id}`);
  }
}
