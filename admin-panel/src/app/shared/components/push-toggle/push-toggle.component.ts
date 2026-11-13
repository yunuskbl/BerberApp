import { Component, Input, OnInit, signal } from '@angular/core';
import { PushService } from '../../../core/services/push.service';

/**
 * "Bu cihazda bildirimler" — Ayarlar'da kart gövdesi, personel panelinde
 * (compact) tek satırlık şerit.
 */
@Component({
  selector: 'app-push-toggle',
  standalone: true,
  template: `
    @switch (push.state()) {
      @case ('unsupported') {
        @if (!compact) {
          <p class="pt-note">Bu tarayıcı anlık bildirimleri desteklemiyor. Android'de Chrome ya da
            iPhone'da ana ekrana eklenmiş ayarlıyo uygulamasını kullanın.</p>
        }
      }
      @case ('ios-install') {
        <div class="pt" [class.pt-compact]="compact">
          <p class="pt-note">iPhone'da bildirimler, ayarlıyo <b>ana ekrana eklendiğinde</b> çalışır:
            Safari'de <b>Paylaş</b> → <b>Ana Ekrana Ekle</b>, sonra uygulamayı ana ekrandan açıp bildirimleri buradan açın.</p>
        </div>
      }
      @case ('denied') {
        <div class="pt" [class.pt-compact]="compact">
          <p class="pt-note pt-warn">Bu cihazda bildirim izni kapalı. Telefonun ayarlarından
            (<b>Ayarlar → Uygulamalar → ayarlıyo → Bildirimler</b>) ya da tarayıcıda adres çubuğundaki
            kilit simgesinden izin verip sayfayı yenileyin.</p>
        </div>
      }
      @case ('off') {
        <div class="pt" [class.pt-compact]="compact">
          <div class="pt-text">
            <strong>🔔 Yeni randevuları anında görün</strong>
            <span>{{ audience === 'staff'
                ? 'Size alınan yeni randevular ve müşteri iptalleri bu cihaza bildirim olarak gelir.'
                : 'Yeni randevu, müşteri iptali ve paket seans onayı bu cihaza bildirim olarak gelir.' }}</span>
          </div>
          <button type="button" class="pt-btn pt-primary" [disabled]="push.busy()" (click)="enable()">
            {{ push.busy() ? 'Açılıyor…' : 'Bildirimleri aç' }}
          </button>
        </div>
      }
      @case ('on') {
        <div class="pt" [class.pt-compact]="compact" [class.pt-row]="compact">
          <div class="pt-text">
            <strong class="pt-on">✓ Bu cihazda bildirimler açık</strong>
            @if (!compact) {
              <span>Diğer cihazlarınızda ayrıca açmanız gerekir.</span>
            }
          </div>
          <div class="pt-actions">
            @if (!compact) {
              <button type="button" class="pt-btn" [disabled]="testing()" (click)="test()">
                {{ testing() ? 'Gönderiliyor…' : 'Deneme gönder' }}
              </button>
            }
            <button type="button" class="pt-btn pt-ghost" [disabled]="push.busy()" (click)="disable()">Kapat</button>
          </div>
        </div>
      }
    }
    @if (message(); as m) {
      <p class="pt-msg" [class.pt-err]="!messageOk()">{{ m }}</p>
    }
  `,
  styles: [`
    :host { display: block; font-family: 'Inter', sans-serif; }
    .pt { display: flex; gap: 14px; align-items: center; justify-content: space-between; flex-wrap: wrap; }
    .pt-text { display: grid; gap: 3px; min-width: 0; flex: 1 1 240px; }
    .pt-text strong { font-size: 14.5px; color: #111827; }
    .pt-text span { font-size: 13px; color: #6b7280; line-height: 1.45; }
    .pt-on { color: #047857 !important; }
    .pt-actions { display: flex; gap: 8px; flex-wrap: wrap; }
    .pt-btn {
      height: 38px; padding: 0 14px; border-radius: 10px; border: 1px solid #e5e7eb;
      background: #fff; color: #111827; font: 600 13.5px 'Inter', sans-serif; cursor: pointer; white-space: nowrap;
    }
    .pt-btn:disabled { opacity: .6; cursor: default; }
    .pt-primary { background: #111827; border-color: #111827; color: #fff; }
    .pt-ghost { color: #6b7280; }
    .pt-note { margin: 0; font-size: 13px; color: #4b5563; line-height: 1.5; }
    .pt-note b { color: #111827; }
    .pt-warn { color: #92400e; }
    .pt-msg { margin: 10px 0 0; font-size: 13px; color: #047857; }
    .pt-err { color: #b91c1c; }
    .pt-row { flex-wrap: nowrap; }
    .pt-row .pt-text { flex: 1 1 0; }
    .pt-compact {
      padding: 12px 14px; border-radius: 14px; background: #f9fafb; border: 1px solid #e5e7eb; margin-bottom: 16px;
    }
  `],
})
export class PushToggleComponent implements OnInit {
  /** Açıklama metni için: işletme sahibi mi personel mi. */
  @Input() audience: 'owner' | 'staff' = 'owner';
  /** Personel panelinde sayfa üstünde tek satır. */
  @Input() compact = false;

  readonly testing = signal(false);
  readonly message = signal<string | null>(null);
  readonly messageOk = signal(true);

  constructor(readonly push: PushService) {}

  ngOnInit(): void {
    this.push.init();
  }

  async enable(): Promise<void> {
    this.message.set(null);
    const r = await this.push.enable();
    if (r.message) this.show(r.message, false);
  }

  async disable(): Promise<void> {
    this.message.set(null);
    await this.push.disable();
  }

  async test(): Promise<void> {
    this.testing.set(true);
    const r = await this.push.sendTest();
    this.testing.set(false);
    this.show(r.ok ? 'Deneme bildirimi gönderildi; birkaç saniye içinde gelmesi gerekir.' : r.message, r.ok);
  }

  private show(text: string, ok: boolean): void {
    this.messageOk.set(ok);
    this.message.set(text);
  }
}
