import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-cookie-consent',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './cookie-consent.component.html',
  styleUrl: './cookie-consent.component.scss'
})
export class CookieConsentComponent implements OnInit {
  visible = false;

  ngOnInit(): void {
    const consent = localStorage.getItem('cookie_consent');
    if (!consent) {
      setTimeout(() => this.visible = true, 800);
    }
  }

  accept(): void {
    localStorage.setItem('cookie_consent', 'accepted');
    this.visible = false;
  }

  decline(): void {
    localStorage.setItem('cookie_consent', 'declined');
    this.visible = false;
  }
}
