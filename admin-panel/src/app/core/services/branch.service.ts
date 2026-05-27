import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface Branch {
  id: string;
  name: string;
  subdomain: string;
  phone?: string;
  address?: string;
  city?: string;
  isActive: boolean;
  createdAt: string;
}

export interface CreateBranchRequest {
  name: string;
  subdomain: string;
  phone?: string;
  address?: string;
  city?: string;
}

export interface ActiveBranch {
  id: string;
  name: string;
}

const STORAGE_KEY = 'activeBranch';

@Injectable({ providedIn: 'root' })
export class BranchService {
  private apiUrl = `${environment.apiUrl}/tenants/branches`;

  activeBranch = signal<ActiveBranch | null>(this.loadFromStorage());

  constructor(private http: HttpClient) {}

  private loadFromStorage(): ActiveBranch | null {
    try {
      const raw = localStorage.getItem(STORAGE_KEY);
      return raw ? JSON.parse(raw) : null;
    } catch { return null; }
  }

  setActiveBranch(branch: ActiveBranch): void {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(branch));
    this.activeBranch.set(branch);
  }

  clearActiveBranch(): void {
    localStorage.removeItem(STORAGE_KEY);
    this.activeBranch.set(null);
  }

  getAll(): Observable<any> {
    return this.http.get<any>(this.apiUrl);
  }

  create(data: CreateBranchRequest): Observable<any> {
    return this.http.post<any>(this.apiUrl, data);
  }

  delete(id: string): Observable<any> {
    return this.http.delete<any>(`${this.apiUrl}/${id}`);
  }
}
