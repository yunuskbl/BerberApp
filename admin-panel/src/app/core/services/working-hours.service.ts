import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface WorkingHour {
  id?:        string;
  staffId:    string;
  dayOfWeek:  number;
  startTime:  string;
  endTime:    string;
  isOff:      boolean;
}

@Injectable({ providedIn: 'root' })
export class WorkingHoursService {
  private apiUrl = `${environment.apiUrl}/workinghours`;

  constructor(private http: HttpClient) {}

  getByStaff(staffId: string): Observable<any> {
    return this.http.get(`${this.apiUrl}/staff/${staffId}`);
  }

  create(data: WorkingHour): Observable<any> {
    return this.http.post(this.apiUrl, data);
  }

  update(id: string, data: WorkingHour): Observable<any> {
    return this.http.put(`${this.apiUrl}/${id}`, data);
  }
}