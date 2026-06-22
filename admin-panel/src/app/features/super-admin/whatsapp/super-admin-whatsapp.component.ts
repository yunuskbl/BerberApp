import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../environments/environment';

type ConnStatus = 'loading' | 'connected' | 'disconnected' | 'error' | 'no_token';

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

  private statusInterval?: ReturnType<typeof setInterval>;
  private qrInterval?: ReturnType<typeof setInterval>;

  constructor(private http: HttpClient) {}

  ngOnInit(): void {
    this.checkStatus();
    this.statusInterval = setInterval(() => this.checkStatus(), 15000);
  }

  ngOnDestroy(): void {
    clearInterval(this.statusInterval);
    clearInterval(this.qrInterval);
  }

  checkStatus(): void {
    this.http.get<any>(`${this.api}/status`).subscribe({
      next: res => {
        if (res.status === 'NO_TOKEN') { this.status = 'no_token'; return; }
        this.status    = res.connected ? 'connected' : 'disconnected';
        this.statusRaw = res.status ?? '';
        if (this.status === 'disconnected' && !this.qrImage) {
          this.loadQr();
          this.qrInterval = setInterval(() => this.loadQr(), 20000);
        }
        if (this.status === 'connected') {
          clearInterval(this.qrInterval);
          this.qrImage = '';
        }
      },
      error: () => { this.status = 'error'; }
    });
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
