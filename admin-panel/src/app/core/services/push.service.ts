import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { SwPush } from '@angular/service-worker';
import { firstValueFrom } from 'rxjs';
import { ApiResponse } from '../models/api-response.model';
import { environment } from '../../../environments/environment';
import { PwaInstallService } from './pwa-install.service';

/**
 * Bu cihazdaki anlık bildirim durumu:
 * - hidden: sunucuda bildirim yapılandırılmamış ya da henüz bilinmiyor → hiçbir şey gösterme
 * - unsupported: tarayıcı desteklemiyor
 * - ios-install: iPhone'da yalnızca ana ekrana eklenmiş uygulamada çalışır
 * - denied: kullanıcı izni reddetti (yalnızca tarayıcı ayarlarından açılabilir)
 * - off / on
 */
export type PushState = 'hidden' | 'unsupported' | 'ios-install' | 'denied' | 'off' | 'on';

interface PushConfig { enabled: boolean; publicKey: string | null; }

/**
 * Panelin anlık bildirimleri (yeni randevu, iptal, seans onayı).
 *
 * Abonelik cihaza özeldir: aynı hesap telefonda açıp bilgisayarda kapalı
 * tutabilir. Açık anahtar önceden alınır; çünkü iPhone izin penceresini
 * yalnızca doğrudan dokunuşla açar, araya bir ağ isteği girerse pencere
 * hiç çıkmaz.
 */
@Injectable({ providedIn: 'root' })
export class PushService {
  private readonly api = `${environment.apiUrl}/push`;
  private publicKey: string | null = null;
  private loaded: Promise<void> | null = null;

  readonly state = signal<PushState>('hidden');
  readonly busy = signal(false);

  constructor(private http: HttpClient, private swPush: SwPush) {}

  /** Durumu bir kez yükler (bileşenler aynı sonucu paylaşır). */
  init(): Promise<void> {
    return this.loaded ??= this.refresh();
  }

  async refresh(): Promise<void> {
    try {
      const res = await firstValueFrom(this.http.get<ApiResponse<PushConfig>>(`${this.api}/config`));
      if (!res.data?.enabled || !res.data.publicKey) { this.state.set('hidden'); return; }
      this.publicKey = res.data.publicKey;
    } catch {
      this.state.set('hidden');
      return;
    }

    if (!PushService.browserSupports() || !this.swPush.isEnabled) {
      this.state.set(PushService.isIos() && !PwaInstallService.isStandalone() ? 'ios-install' : 'unsupported');
      return;
    }
    if (Notification.permission === 'denied') { this.state.set('denied'); return; }

    const sub = await this.currentSubscription();
    if (sub && Notification.permission === 'granted') {
      // Sunucudaki kaydı tazele: geçersiz sayılıp silinmiş olabilir ya da bu
      // cihazda artık başka bir hesap açık olabilir.
      try { await this.register(sub); this.state.set('on'); }
      catch { this.state.set('off'); }
      return;
    }
    this.state.set('off');
  }

  /** İzin ister ve cihazı kaydeder. Bir dokunuşun içinden çağrılmalı. */
  async enable(): Promise<{ ok: boolean; message?: string }> {
    if (!this.publicKey) return { ok: false, message: 'Bildirimler şu an kullanılamıyor.' };
    this.busy.set(true);
    try {
      const permission = await Notification.requestPermission();
      if (permission !== 'granted') {
        this.state.set(permission === 'denied' ? 'denied' : 'off');
        return { ok: false };
      }
      // Service worker sayfa sakinleşince (en geç 30 sn) kaydoluyor; ilk
      // ziyarette düğmeye ondan önce basılırsa beklememek için hemen kaydet.
      // Aynı betik ve kapsam olduğu için mevcut kaydı döndürür, ikinci bir çalışan açmaz.
      if (!navigator.serviceWorker.controller) {
        await navigator.serviceWorker.register('ngsw-worker.js').catch(() => null);
      }
      const sub = await Promise.race([
        this.swPush.requestSubscription({ serverPublicKey: this.publicKey }),
        new Promise<never>((_, reject) => setTimeout(() => reject({
          error: { message: 'Bildirim altyapısı hazırlanamadı. Sayfayı yenileyip tekrar deneyin.' },
        }), 45_000)),
      ]);
      await this.register(sub);
      this.state.set('on');
      return { ok: true };
    } catch (e: any) {
      return { ok: false, message: e?.error?.message || 'Bildirimler açılamadı. Lütfen tekrar deneyin.' };
    } finally {
      this.busy.set(false);
    }
  }

  async disable(): Promise<void> {
    this.busy.set(true);
    try {
      const sub = await this.currentSubscription();
      if (sub) {
        await firstValueFrom(this.http.post(`${this.api}/unsubscribe`, { endpoint: sub.endpoint })).catch(() => null);
        await sub.unsubscribe().catch(() => false);
      }
      this.state.set('off');
    } finally {
      this.busy.set(false);
    }
  }

  async sendTest(): Promise<{ ok: boolean; message: string }> {
    try {
      const res = await firstValueFrom(this.http.post<ApiResponse<{ delivered: number }>>(`${this.api}/test`, {}));
      return { ok: true, message: res.message || 'Deneme bildirimi gönderildi.' };
    } catch (e: any) {
      return { ok: false, message: e?.error?.message || 'Deneme bildirimi gönderilemedi.' };
    }
  }

  private async register(sub: PushSubscription): Promise<void> {
    const json = sub.toJSON();
    await firstValueFrom(this.http.post(`${this.api}/subscribe`, {
      endpoint: json.endpoint,
      keys: json.keys,
      standalone: PwaInstallService.isStandalone(),
      platform: PushService.platform(),
    }));
  }

  private async currentSubscription(): Promise<PushSubscription | null> {
    const reg = await navigator.serviceWorker.getRegistration();
    return (await reg?.pushManager.getSubscription()) ?? null;
  }

  /**
   * Çıkış yapınca bu cihaz artık o hesabın bildirimlerini almasın. Sunucuya
   * istek gerekmez: tarayıcı aboneliği bırakınca push sunucusu sonraki
   * gönderimde 410 döner ve kayıt kendiliğinden silinir.
   */
  static forgetDevice(): void {
    try {
      navigator.serviceWorker?.getRegistration()
        .then(reg => reg?.pushManager?.getSubscription())
        .then(sub => sub?.unsubscribe())
        .catch(() => { /* önemli değil */ });
    } catch { /* desteklenmiyor */ }
  }

  static browserSupports(): boolean {
    return 'serviceWorker' in navigator && 'PushManager' in window && 'Notification' in window;
  }

  static isIos(): boolean {
    const ua = navigator.userAgent;
    return /iPhone|iPad|iPod/i.test(ua) || (navigator.platform === 'MacIntel' && navigator.maxTouchPoints > 1);
  }

  private static platform(): string {
    if (PushService.isIos()) return 'ios';
    if (/Android/i.test(navigator.userAgent)) return 'android';
    return 'desktop';
  }
}
