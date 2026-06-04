import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface SubscriptionDto {
  plan: string;
  status: string;
  expiryDate: string;
  price: number;
  currency: string;
  autoRenewal: boolean;
  isActive: boolean;
}

@Injectable({ providedIn: 'root' })
export class SubscriptionService {
  private base = `${environment.apiUrl}/subscription`;

  constructor(private http: HttpClient) {}

  getSubscription(): Observable<{ success: boolean; data: SubscriptionDto | null }> {
    return this.http.get<any>(this.base);
  }

  initCheckout(plan: string): Observable<{ success: boolean; data: { checkoutFormContent: string; token: string } }> {
    return this.http.post<any>(`${this.base}/checkout`, { plan });
  }

  cancelSubscription(): Observable<{ success: boolean; message: string }> {
    return this.http.delete<any>(this.base);
  }
}
