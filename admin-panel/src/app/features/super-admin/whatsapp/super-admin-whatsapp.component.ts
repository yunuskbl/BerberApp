import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../environments/environment';

type ConnStatus = 'loading' | 'connected' | 'disconnected' | 'error' | 'no_token';

/** Kendi numarasını bağlamış bir işletmenin hat durumu. */
interface TenantSession {
  id:          string;
  name:        string;
  subdomain:   string | null;
  session:     string;
  status:      string;
  isConnected: boolean;
  isReachable: boolean;
  isDown:      boolean;
}

@Component({
  selector: 'app-super-admin-whatsapp',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './super-admin-whatsapp.component.html',
  styleUrl: './super-admin-whatsapp.component.scss'
})
export class SuperAdminWhatsAppComponent implements OnInit, OnDestroy {
  private api = `${environment.apiUrl}/superadmin/whatsapp`;

  status: ConnStatus = 'loading';
  statusRaw = '';
  qrImage = '';
  qrLoading = false;
  qrError = '';
  actionLoading = false;
  actionMsg = '';
  generatedToken = '';

  /** İşletme hatları — merkezi oturumdan ayrı, canlı sorgulanıyor. */
  tenantSessions: TenantSession[] = [];
  tenantsDown = 0;
  tenantsUnknown = 0;
  tenantsLoading = false;

  private statusInterval?: ReturnType<typeof setInterval>;
  private qrInterval?: ReturnType<typeof setInterval>;

  constructor(private http: HttpClient) {}

  ngOnInit(): void {
    // İşletme hatları açılışta sorgulanmıyor: her satır WPPConnect'e ayrı bir
    // istek demek ve merkezi oturumun QR'ı zaten aynı servisi bekliyor. Sayfanın
    // ilk işi merkezi bağlantı; hatlara "Yenile" ile bakılıyor.
    this.checkStatus();
    this.statusInterval = setInterval(() => this.checkStatus(), 15000);
  }

  ngOnDestroy(): void {
    clearInterval(this.statusInterval);
    this.stopQrPolling();
  }

  /**
   * İşletmelerin kendi hatları. Her satır WPPConnect'e bir istek demek, bu
   * yüzden merkezi durum gibi 15 saniyede bir değil, elle yenileniyor.
   */
  loadTenantSessions(): void {
    this.tenantsLoading = true;
    this.http.get<any>(`${this.api}/tenant-sessions`).subscribe({
      next: res => {
        if (res?.success) {
          this.tenantSessions = res.data.sessions ?? [];
          this.tenantsDown    = res.data.down ?? 0;
          this.tenantsUnknown = res.data.unknown ?? 0;
        }
        this.tenantsLoading = false;
      },
      error: () => { this.tenantsLoading = false; },
    });
  }

  checkStatus(): void {
    this.http.get<any>(`${this.api}/status`).subscribe({
      next: res => {
        if (res.status === 'NO_TOKEN') { this.status = 'no_token'; return; }
        this.status    = res.connected ? 'connected' : 'disconnected';
        this.statusRaw = res.status ?? '';
        if (this.status === 'disconnected' && !this.qrImage) {
          this.startQrPolling();
        }
        if (this.status === 'connected') {
          this.stopQrPolling();
          this.qrImage = '';
        }
      },
      error: () => { this.status = 'error'; }
    });
  }

  /**
   * QR beklerken tek bir zamanlayıcı çalışır.
   *
   * Eskiden durum kontrolü (15 sn) her seferinde yeni bir 20 saniyelik interval
   * kuruyordu, öncekini temizlemeden. Birkaç dakika sonra onlarca zamanlayıcı
   * birikip saniyede bir /qr isteği atıyordu; her istek de oturumu yeniden
   * başlattığı için QR üretilecek kadar ilerleyemiyordu. Merkezi oturumun
   * dosyaları bu yüzden günlerce güncellenmemişti.
   */
  private startQrPolling(): void {
    if (this.qrInterval) return;          // zaten bekliyoruz
    this.loadQr();
    this.qrInterval = setInterval(() => {
      if (this.qrImage) { this.stopQrPolling(); return; }
      this.loadQr();
    }, 6000);
  }

  private stopQrPolling(): void {
    if (this.qrInterval) {
      clearInterval(this.qrInterval);
      this.qrInterval = undefined;
    }
  }

  loadQr(): void {
    this.qrLoading = true;
    this.qrError = '';
    this.http.get<any>(`${this.api}/qr`).subscribe({
      next: res => {
        this.qrLoading = false;
        if (res.success && res.qr) {
          this.qrImage = res.qr;
        } else {
          this.qrError = res.message ?? 'QR alınamadı.';
        }
      },
      error: () => { this.qrLoading = false; this.qrError = 'QR isteği başarısız.'; }
    });
  }

  startSession(): void {
    this.actionLoading = true;
    this.actionMsg = '';
    this.http.post<any>(`${this.api}/start`, {}).subscribe({
      next: () => {
        this.actionLoading = false;
        this.actionMsg = 'Oturum başlatıldı. QR kodu tarayın.';
        this.loadQr();
      },
      error: () => { this.actionLoading = false; this.actionMsg = 'Hata oluştu.'; }
    });
  }

  disconnect(): void {
    if (!confirm('WhatsApp bağlantısını kesmek istediğinizden emin misiniz?')) return;
    this.actionLoading = true;
    this.http.post<any>(`${this.api}/disconnect`, {}).subscribe({
      next: () => {
        this.actionLoading = false;
        this.actionMsg = 'Bağlantı kesildi.';
        this.status = 'disconnected';
        this.qrImage = '';
        setTimeout(() => this.checkStatus(), 1500);
      },
      error: () => { this.actionLoading = false; this.actionMsg = 'Hata oluştu.'; }
    });
  }

  generateToken(): void {
    this.actionLoading = true;
    this.actionMsg = '';
    this.generatedToken = '';
    this.http.post<any>(`${this.api}/generate-token`, {}).subscribe({
      next: res => {
        this.actionLoading = false;
        if (res.success) {
          this.generatedToken = res.token ?? '';
          this.actionMsg = res.message ?? 'Token oluşturuldu.';
          // Token kaydedildi, QR'ı otomatik yükle
          setTimeout(() => {
            this.status = 'disconnected';
            this.loadQr();
          }, 1000);
        } else {
          this.actionMsg = res.message ?? 'Token oluşturulamadı.';
        }
      },
      error: () => { this.actionLoading = false; this.actionMsg = 'Hata oluştu.'; }
    });
  }

  refreshQr(): void {
    this.qrImage = '';
    this.loadQr();
  }
}
