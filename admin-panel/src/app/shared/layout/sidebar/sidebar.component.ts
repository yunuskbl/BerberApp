import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { Router } from '@angular/router';
import { LanguageService, Lang } from '../../../core/services/language.service';
import { TranslatePipe } from '../../pipes/translate.pipe';

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [CommonModule, RouterModule, TranslatePipe],
  templateUrl: './sidebar.component.html',
  styleUrl: './sidebar.component.scss'
})
export class SidebarComponent {
  isCollapsed = false;

  menuItems = [
    { key: 'nav.home',         icon: '▦',  route: '/dashboard'    },
    { key: 'nav.appointments', icon: '📅', route: '/appointments' },
    { key: 'nav.staff',        icon: '👤', route: '/staff'        },
    { key: 'nav.services',     icon: '✂',  route: '/services'     },
    { key: 'nav.customers',    icon: '👥', route: '/customers'    },
    { key: 'nav.reports',      icon: '📊', route: '/reports'      },
  ];

  bottomItems = [
    { key: 'nav.settings', icon: '⚙', route: '/settings' },
  ];

  languages: { code: Lang; flag: string }[] = [
    { code: 'tr', flag: '🇹🇷' },
    { code: 'en', flag: '🇬🇧' },
    { code: 'ru', flag: '🇷🇺' },
  ];

  constructor(
    private authService: AuthService,
    private router: Router,
    public langService: LanguageService,
  ) { }

  get user() { return this.authService.getUser(); }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }

  navigateTo(route: string): void {
    this.router.navigate([route]);
    if (window.innerWidth < 768) {
      this.isCollapsed = true;
    }
  }

  setLang(code: Lang): void {
    this.langService.setLang(code);
  }
}
