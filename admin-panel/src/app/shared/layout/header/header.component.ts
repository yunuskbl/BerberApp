import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, NavigationEnd, RouterModule } from '@angular/router';
import { filter } from 'rxjs/operators';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './header.component.html',
  styleUrl: './header.component.scss'
})
export class HeaderComponent {
  pageTitle    = 'Dashboard';
  pageSubtitle = 'Genel bakış';

  private pageTitles: Record<string, { title: string; subtitle: string }> = {
    '/dashboard':    { title: 'Dashboard',   subtitle: 'Genel bakış ve özet bilgiler'    },
    '/appointments': { title: 'Randevular',  subtitle: 'Randevu yönetimi ve takvim'      },
    '/staff':        { title: 'Personel',    subtitle: 'Personel yönetimi'               },
    '/services':     { title: 'Hizmetler',   subtitle: 'Hizmet ve fiyat yönetimi'        },
    '/customers':    { title: 'Müşteriler',  subtitle: 'Müşteri kayıtları ve geçmiş'     },
    '/settings':     { title: 'Ayarlar',     subtitle: 'Sistem ve hesap ayarları'        },
  };

  constructor(private router: Router) {
    this.updateTitle(this.router.url);

    this.router.events
      .pipe(filter(e => e instanceof NavigationEnd))
      .subscribe((e: any) => this.updateTitle(e.urlAfterRedirects));
  }

  private updateTitle(url: string): void {
    const page = this.pageTitles[url] ?? { title: 'BerberApp', subtitle: '' };
    this.pageTitle    = page.title;
    this.pageSubtitle = page.subtitle;
  }

  get currentDate(): string {
    return new Date().toLocaleDateString('tr-TR', {
      weekday: 'long',
      year:    'numeric',
      month:   'long',
      day:     'numeric'
    });
  }
}