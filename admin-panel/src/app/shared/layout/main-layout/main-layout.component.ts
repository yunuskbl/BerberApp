import { Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { RouterLink, RouterOutlet } from '@angular/router';
import { SidebarComponent } from '../sidebar/sidebar.component';
import { HeaderComponent } from '../header/header.component';
import { BottomNavComponent } from '../../components/bottom-nav/bottom-nav.component';
import { AuthService } from '../../../core/services/auth.service';
import { LanguageService } from '../../../core/services/language.service';
import { environment } from '../../../../environments/environment';

/** İşletmenin kendi WhatsApp hattının durumu. */
interface WhatsAppStatus {
  status:                   string;
  isConnected:              boolean;
  isReachable:              boolean;
  isDefinitelyDisconnected: boolean;
  hasSession:               boolean;
  usesOwnNumber:            boolean;
}

/** Deneme süresince işletmenin sistemden fiilen ne aldığı. */
interface TrialUsage {
  isOnTrial:             boolean;
  daysLeft:              number;
  completedAppointments: number;
  distinctCustomers:     number;
  notificationsSent:     number;
  revenueInTry:          number;
}

@Component({
  selector: 'app-main-layout',
  standalone: true,
  imports: [CommonModule, RouterOutlet, RouterLink, SidebarComponent, HeaderComponent, BottomNavComponent],
  templateUrl: './main-layout.component.html',
  styleUrl: './main-layout.component.scss'
})
export class MainLayoutComponent implements OnInit, OnDestroy {
  isSidebarCollapsed = false;

  /** Deneme kullanım özeti; gelene kadar şerit yalnızca kalan günü gösterir. */
  usage: TrialUsage | null = null;

  /** WhatsApp hattı; null ise henüz bilinmiyor. */
  whatsApp: WhatsAppStatus | null = null;
  private whatsAppTimer?: ReturnType<typeof setInterval>;

  constructor(
    public authService: AuthService,
    public langService: LanguageService,
    private http: HttpClient,
  ) {}

  ngOnInit(): void {
    this.checkWhatsApp();
    // Hat koptuğunda uyarı ekranda kalmalı ama bağlantı kurulunca da
    // kendiliğinden kalkmalı; beş dakikada bir yeniden soruluyor.
    this.whatsAppTimer = setInterval(() => this.checkWhatsApp(), 5 * 60_000);

    // Deneme özeti yalnızca denemedeyken sorulur; ödeyen işletmeye gereksiz
    // istek atılmaz.
    if (!this.isOnTrial) return;

    this.http.get<any>(`${environment.apiUrl}/subscription/trial-summary`).subscribe({
      next: (res) => {
        if (res?.success && res.data?.isOnTrial) this.usage = res.data;
      },
      // Özet gelmezse şerit sade hâliyle çalışmaya devam eder.
      error: () => {},
    });
  }

  ngOnDestroy(): void {
    if (this.whatsAppTimer) clearInterval(this.whatsAppTimer);
  }

  private checkWhatsApp(): void {
    this.http.get<any>(`${environment.apiUrl}/tenants/whatsapp/status`).subscribe({
      next: (res) => { if (res?.success) this.whatsApp = res.data; },
      // Durum alınamadıysa uyarı gösterilmiyor: bilmemek, kopuk olmakla aynı
      // şey değil.
      error: () => { this.whatsApp = null; },
    });
  }

  /**
   * Uyarı yalnızca kendi numarasını bağlamış ve hattının kopuk olduğu
   * <b>kesin</b> olan işletmede çıkıyor. WPPConnect'e ulaşılamadığında
   * ("bilmiyoruz") uyarı basmak, bir süre sonra kimsenin bakmadığı bir şerit
   * üretir.
   */
  get isWhatsAppDown(): boolean {
    return !!this.whatsApp?.usesOwnNumber && this.whatsApp.isDefinitelyDisconnected;
  }

  get trialDaysLeft(): number { return this.authService.getTrialDaysLeft(); }
  get isOnTrial(): boolean    { return this.authService.isOnTrial(); }

  /**
   * Şeritte kanıt gösterilebilir mi. Hiç randevu yoksa rakam basılmıyor:
   * "0 randevu aldınız" yükseltmek için sebep değil, aksini söyler.
   */
  get hasUsageProof(): boolean {
    return (this.usage?.completedAppointments ?? 0) > 0;
  }

  formatMoney(value: number): string {
    return new Intl.NumberFormat(this.langService.dateLocale, {
      maximumFractionDigits: 0,
    }).format(value);
  }
}
