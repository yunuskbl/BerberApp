import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ApiResponse } from '../models/api-response.model';
import {
  PackageDetail, PackagePaymentMethod, PackageSummary, PackageTemplate, PackageTemplateRequest,
  PackageBookingContext, PublicPackage, PublicSessionInfo, SellPackageRequest, SessionConfirm,
} from '../models/package.model';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class PackageService {
  private api    = `${environment.apiUrl}/packages`;
  private public = `${environment.apiUrl}/public/packages`;

  constructor(private http: HttpClient) {}

  // ── Paket tanımları ──
  getTemplates(): Observable<ApiResponse<PackageTemplate[]>> {
    return this.http.get<ApiResponse<PackageTemplate[]>>(`${this.api}/templates`);
  }
  createTemplate(req: PackageTemplateRequest): Observable<ApiResponse<{ id: string }>> {
    return this.http.post<ApiResponse<{ id: string }>>(`${this.api}/templates`, req);
  }
  updateTemplate(id: string, req: PackageTemplateRequest): Observable<ApiResponse<{ id: string }>> {
    return this.http.put<ApiResponse<{ id: string }>>(`${this.api}/templates/${id}`, req);
  }
  deleteTemplate(id: string): Observable<ApiResponse<null>> {
    return this.http.delete<ApiResponse<null>>(`${this.api}/templates/${id}`);
  }

  // ── Satılan paketler ──
  getPackages(status: 'active' | 'completed' | 'cancelled' | 'all', customerId?: string): Observable<ApiResponse<PackageSummary[]>> {
    let params = new HttpParams().set('status', status);
    if (customerId) params = params.set('customerId', customerId);
    return this.http.get<ApiResponse<PackageSummary[]>>(this.api, { params });
  }
  getPackage(id: string): Observable<ApiResponse<PackageDetail>> {
    return this.http.get<ApiResponse<PackageDetail>>(`${this.api}/${id}`);
  }
  sell(req: SellPackageRequest): Observable<ApiResponse<PackageDetail>> {
    return this.http.post<ApiResponse<PackageDetail>>(this.api, req);
  }
  cancel(id: string, reason: string | null): Observable<ApiResponse<PackageDetail>> {
    return this.http.post<ApiResponse<PackageDetail>>(`${this.api}/${id}/cancel`, { reason });
  }

  // ── Seanslar ──
  recordSession(id: string, staffId: string | null, note: string | null): Observable<ApiResponse<PackageDetail>> {
    return this.http.post<ApiResponse<PackageDetail>>(`${this.api}/${id}/sessions`, { staffId, note });
  }
  sessionStatus(id: string, sessionId: string): Observable<ApiResponse<{ id: string; customerConfirmedAt?: string | null; isVoid: boolean; confirmCodeExpiresAt?: string | null }>> {
    return this.http.get<any>(`${this.api}/${id}/sessions/${sessionId}`);
  }
  renewConfirmCode(id: string, sessionId: string): Observable<ApiResponse<SessionConfirm>> {
    return this.http.post<ApiResponse<SessionConfirm>>(`${this.api}/${id}/sessions/${sessionId}/confirm-code`, {});
  }
  voidSession(id: string, sessionId: string, reason: string | null): Observable<ApiResponse<PackageDetail>> {
    return this.http.post<ApiResponse<PackageDetail>>(`${this.api}/${id}/sessions/${sessionId}/void`, { reason });
  }

  // ── Ödemeler ──
  addPayment(id: string, amount: number, method: PackagePaymentMethod, note: string | null): Observable<ApiResponse<PackageDetail>> {
    return this.http.post<ApiResponse<PackageDetail>>(`${this.api}/${id}/payments`, { amount, method, note });
  }
  voidPayment(id: string, paymentId: string): Observable<ApiResponse<PackageDetail>> {
    return this.http.post<ApiResponse<PackageDetail>>(`${this.api}/${id}/payments/${paymentId}/void`, {});
  }

  // ── Müşteri tarafı ──
  getPublicPackage(token: string): Observable<ApiResponse<PublicPackage>> {
    return this.http.get<ApiResponse<PublicPackage>>(`${this.public}/${token}`);
  }
  getPublicSession(code: string): Observable<ApiResponse<PublicSessionInfo>> {
    return this.http.get<ApiResponse<PublicSessionInfo>>(`${this.public}/sessions/${code}`);
  }
  confirmPublicSession(code: string): Observable<ApiResponse<PublicSessionInfo>> {
    return this.http.post<ApiResponse<PublicSessionInfo>>(`${this.public}/sessions/${code}/confirm`, {});
  }
  /** Müşteri seansı takip sayfasından onaylar (salonda QR okutmadıysa). */
  confirmFromTracking(token: string, sessionId: string): Observable<ApiResponse<null>> {
    return this.http.post<ApiResponse<null>>(`${this.public}/${token}/sessions/${sessionId}/confirm`, {});
  }

  // ── Paketten randevu ──
  getBookingContext(token: string): Observable<ApiResponse<PackageBookingContext>> {
    return this.http.get<ApiResponse<PackageBookingContext>>(`${this.public}/${token}/booking`);
  }
  /** Doğrulama kodu girilmiş numaranın o salondaki aktif paketleri. */
  lookupPackages(subdomain: string, phone: string): Observable<ApiResponse<PackageBookingContext[]>> {
    return this.http.post<ApiResponse<PackageBookingContext[]>>(`${this.public}/lookup`, { subdomain, phone });
  }
}
