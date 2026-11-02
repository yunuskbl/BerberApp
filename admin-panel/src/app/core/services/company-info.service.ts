import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, catchError, map, of, shareReplay } from 'rxjs';
import { environment } from '../../../environments/environment';

/** Hizmet sağlayıcının yasal kimliği. Doldurulmamış alanlar null gelir. */
export interface CompanyInfo {
  legalName: string | null;
  address:   string | null;
  email:     string | null;
  phone:     string | null;
  taxInfo:   string | null;
}

/**
 * Sitenin altbilgisinde ve gizlilik politikasında gösterilen yasal kimlik.
 * Kaynak sunucudaki COMPANY_* ayarları — e-posta altbilgisiyle aynı yer.
 * Sayfa başına bir kez sorulur ve önbelleğe alınır.
 */
@Injectable({ providedIn: 'root' })
export class CompanyInfoService {
  readonly info$: Observable<CompanyInfo | null>;

  constructor(http: HttpClient) {
    this.info$ = http.get<any>(`${environment.apiUrl}/company`).pipe(
      map(res => (res?.success ? res.data as CompanyInfo : null)),
      // Kimlik bilgisi gelmezse sayfa yine çalışır; yalnızca o satır basılmaz.
      catchError(() => of(null)),
      shareReplay(1),
    );
  }
}
