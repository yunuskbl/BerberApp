import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, RouterLink, RouterLinkActive } from '@angular/router';
import { LanguageService } from '../../../core/services/language.service';
import { TranslatePipe } from '../../pipes/translate.pipe';

@Component({
  selector: 'app-bottom-nav',
  standalone: true,
  imports: [CommonModule, RouterModule, RouterLink, RouterLinkActive, TranslatePipe],
  template: `
    <nav class="bottom-nav">
      <a routerLink="/dashboard"    routerLinkActive="active" class="bottom-nav-item">
        <span class="nav-icon">🏠</span>
        <span class="nav-label">{{ 'nav.home' | translate }}</span>
      </a>
      <a routerLink="/appointments" routerLinkActive="active" class="bottom-nav-item">
        <span class="nav-icon">📅</span>
        <span class="nav-label">{{ 'nav.appointments' | translate }}</span>
      </a>
      <a routerLink="/staff"        routerLinkActive="active" class="bottom-nav-item">
        <span class="nav-icon">👤</span>
        <span class="nav-label">{{ 'nav.staff' | translate }}</span>
      </a>
      <a routerLink="/services"     routerLinkActive="active" class="bottom-nav-item">
        <span class="nav-icon">✂</span>
        <span class="nav-label">{{ 'nav.services' | translate }}</span>
      </a>
      <a routerLink="/reports"      routerLinkActive="active" class="bottom-nav-item">
        <span class="nav-icon">📊</span>
        <span class="nav-label">{{ 'nav.reports' | translate }}</span>
      </a>
      <a routerLink="/settings"     routerLinkActive="active" class="bottom-nav-item">
        <span class="nav-icon">⚙️</span>
        <span class="nav-label">{{ 'nav.settings' | translate }}</span>
      </a>
    </nav>
  `,
  styles: [`
    .bottom-nav {
      display: none;
      position: fixed;
      bottom: 0; left: 0; right: 0;
      height: calc(60px + env(safe-area-inset-bottom, 0px));
      background: #0f172a;
      border-top: 1px solid rgba(255,255,255,0.08);
      z-index: 1000;
      padding: 0 4px;
      padding-bottom: env(safe-area-inset-bottom, 0px);
      -webkit-transform: translateZ(0);
      transform: translateZ(0);
      will-change: transform;

      @media (max-width: 768px) {
        display: flex;
        align-items: center;
        justify-content: space-around;
      }
    }

    .bottom-nav-item {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 2px;
      padding: 6px 8px;
      border-radius: 8px;
      color: rgba(255,255,255,0.4);
      text-decoration: none;
      transition: all 0.2s;
      flex: 1;
      max-width: 64px;

      .nav-icon  { font-size: 18px; line-height: 1; }
      .nav-label { font-size: 9px; font-weight: 500; white-space: nowrap; }

      &.active {
        color: #a78bfa;
        background: rgba(167,139,250,0.12);
      }
    }
  `],
})
export class BottomNavComponent {
  constructor(public langService: LanguageService) {}
}
