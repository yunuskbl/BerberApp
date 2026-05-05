import { Component, Input, HostBinding } from '@angular/core';

@Component({
  selector: 'app-decorative-bg',
  standalone: true,
  templateUrl: './decorative-bg.component.html',
  styleUrl: './decorative-bg.component.scss',
})
export class DecorativeBgComponent {
  /** Tema rengi — CSS custom property olarak SVG'ye yansır */
  @Input() accent = '#c9a96e';

  @HostBinding('style.--bg-accent')
  get accentVar(): string { return this.accent; }
}
