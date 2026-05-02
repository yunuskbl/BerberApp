import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LanguageService, Lang } from '../../../core/services/language.service';

@Component({
  selector: 'app-language-switcher',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="lang-switcher">
      @for (l of languages; track l.code) {
        <button
          class="lang-btn"
          [class.active]="langService.lang() === l.code"
          (click)="setLang(l.code)"
          [title]="l.label"
        >
          <span class="lang-flag">{{ l.flag }}</span>
          <span class="lang-label">{{ l.label }}</span>
        </button>
      }
    </div>
  `,
  styles: [`
    .lang-switcher {
      display: flex;
      align-items: center;
      gap: 4px;
      background: #f1f5f9;
      border-radius: 10px;
      padding: 3px;
    }

    .lang-btn {
      display: flex;
      align-items: center;
      gap: 5px;
      padding: 5px 10px;
      border: none;
      border-radius: 7px;
      background: transparent;
      color: #64748b;
      font-size: 12px;
      font-weight: 600;
      cursor: pointer;
      transition: all 0.18s;
      font-family: 'Inter', sans-serif;
      line-height: 1;
    }

    .lang-flag {
      font-size: 15px;
      line-height: 1;
    }

    .lang-label {
      letter-spacing: 0.3px;
    }

    .lang-btn:hover:not(.active) {
      background: rgba(255,255,255,0.7);
      color: #334155;
    }

    .lang-btn.active {
      background: white;
      color: #0f172a;
      box-shadow: 0 1px 4px rgba(0,0,0,0.10);
    }
  `],
})
export class LanguageSwitcherComponent {
  languages: { code: Lang; flag: string; label: string }[] = [
    { code: 'tr', flag: '🇹🇷', label: 'TR' },
    { code: 'en', flag: '🇬🇧', label: 'EN' },
    { code: 'ru', flag: '🇷🇺', label: 'RU' },
  ];

  constructor(public langService: LanguageService) {}

  setLang(code: Lang): void {
    this.langService.setLang(code);
  }
}
