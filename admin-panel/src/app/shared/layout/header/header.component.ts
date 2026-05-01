import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, NavigationEnd, RouterModule } from '@angular/router';
import { filter } from 'rxjs/operators';
import { LanguageService, Lang } from '../../../core/services/language.service';
import { TranslatePipe } from '../../pipes/translate.pipe';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [CommonModule, RouterModule, TranslatePipe],
  templateUrl: './header.component.html',
  styleUrl: './header.component.scss',
})
export class HeaderComponent {
  currentPath = '/dashboard';

  languages: { code: Lang; flag: string; label: string }[] = [
    { code: 'tr', flag: '🇹🇷', label: 'TR' },
    { code: 'en', flag: '🇬🇧', label: 'EN' },
    { code: 'ru', flag: '🇷🇺', label: 'RU' },
  ];

  private pathToKey: Record<string, string> = {
    '/dashboard':    'page.dashboard',
    '/appointments': 'page.appointments',
    '/staff':        'page.staff',
    '/services':     'page.services',
    '/customers':    'page.customers',
    '/settings':     'page.settings',
    '/reports':      'page.reports',
  };

  constructor(
    private router: Router,
    public langService: LanguageService,
  ) {
    this.currentPath = this.router.url;
    this.router.events
      .pipe(filter(e => e instanceof NavigationEnd))
      .subscribe((e: any) => (this.currentPath = e.urlAfterRedirects));
  }

  get pageTitleKey(): string {
    return this.pathToKey[this.currentPath] ?? 'page.dashboard';
  }

  get currentDate(): string {
    return new Date().toLocaleDateString(this.langService.dateLocale, {
      weekday: 'long',
      year:    'numeric',
      month:   'long',
      day:     'numeric',
    });
  }

  setLang(code: Lang): void {
    this.langService.setLang(code);
  }
}
