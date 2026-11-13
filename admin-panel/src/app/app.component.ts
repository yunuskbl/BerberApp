import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { CookieConsentComponent } from './shared/components/cookie-consent/cookie-consent.component';
import { AppUpdateComponent } from './shared/components/app-update/app-update.component';
import { PwaInstallService } from './core/services/pwa-install.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, CookieConsentComponent, AppUpdateComponent],
  template: `
    <router-outlet />
    <app-cookie-consent />
    <app-update />
  `
})
export class AppComponent {
  // Kurulum olayı (beforeinstallprompt) sayfa açılır açılmaz gelebilir;
  // servisi burada oluşturmak onu kaçırmamayı sağlıyor.
  constructor(_pwa: PwaInstallService) {}
}