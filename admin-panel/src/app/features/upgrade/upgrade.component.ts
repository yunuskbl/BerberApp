// upgrade.component.ts
import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-upgrade',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="upgrade-container">
      <div class="upgrade-card">
        <h2>⛔ Yetki Yetersiz</h2>
        <p>
          Bu özellik <strong>{{ requiredPlan }}</strong> paket'te mevcut
        </p>
        <p>
          Sizin paketiniz: <strong>{{ currentPlan }}</strong>
        </p>
        <button (click)="goToPricing()" class="btn-upgrade">
          Paket Yükselt
        </button>
      </div>
    </div>
  `,
  styles: [`
    .upgrade-container {
      display: flex;
      justify-content: center;
      align-items: center;
      height: 100vh;
    }
    
    .upgrade-card {
      text-align: center;
      padding: 40px;
      background: #f5f5f5;
      border-radius: 8px;
      box-shadow: 0 2px 8px rgba(0,0,0,0.1);
    }
    
    h2 { font-size: 24px; margin-bottom: 20px; }
    p { margin: 10px 0; font-size: 16px; }
    
    .btn-upgrade {
      margin-top: 20px;
      padding: 10px 20px;
      background: #6366f1;
      color: white;
      border: none;
      border-radius: 4px;
      cursor: pointer;
      font-size: 16px;
    }
    
    .btn-upgrade:hover { background: #4f46e5; }
  `]
})
export class UpgradeComponent implements OnInit {
  currentPlan = 'Basic';
  requiredPlan = 'Standard';

  constructor(private route: ActivatedRoute) {}

  ngOnInit() {
    this.route.queryParams.subscribe(params => {
      this.currentPlan = params['current'] || 'Basic';
      this.requiredPlan = params['required'] || 'Standard';
    });
  }

  goToPricing() {
    // Pricing sayfasına yönlendir
    window.location.href = '/pricing';
  }
}