import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';

@Component({
  selector: 'app-payment',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './payment.component.html',
  styleUrl: './payment.component.scss'
})
export class PaymentComponent implements OnInit {
  plan = '';
  planLabel = '';
  price = 0;

  state: 'loading' | 'bank-info' | 'submitting' | 'submitted' | 'fail' = 'loading';
  errorMessage = '';
  referenceCode = '';

  readonly planMap: Record<string, { label: string; price: number; icon: string }> = {
    baslangic:   { label: 'Başlangıç',   price: 899,  icon: '🌱' },
    profesyonel: { label: 'Profesyonel', price: 1799, icon: '⚡' },
    premium:     { label: 'Premium',     price: 2999, icon: '👑' },
  };

  readonly bankInfo = {
    bankName:      'Garanti Bankası',
    iban:          'TR97 0006 2000 5770 0006 6537 82',
    accountHolder: 'Yunus Emre Kobal',
    description:   'ayarlıyo abonelik ödemesi',
  };

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private http: HttpClient
  ) {}

  ngOnInit(): void {
    this.route.queryParamMap.subscribe(params => {
      this.plan = params.get('plan') ?? '';
      const info = this.planMap[this.plan];
      if (!info) { this.router.navigate(['/pricing']); return; }
      this.planLabel = info.label;
      this.price     = info.price;
      this.state     = 'bank-info';
    });
  }

  confirmTransfer(): void {
    this.state = 'submitting';
    this.http.post<any>(`${environment.apiUrl}/payment-request/submit`, {
      planName:  this.plan,
      planLabel: this.planLabel,
      amount:    this.price
    }).subscribe({
      next: res => {
        if (res.success) {
          this.referenceCode = res.referenceCode;
          this.state = 'submitted';
        } else {
          this.errorMessage = res.message ?? 'Bir hata oluştu.';
          this.state = 'fail';
        }
      },
      error: () => {
        this.errorMessage = 'Sunucu hatası. Lütfen tekrar deneyin.';
        this.state = 'fail';
      }
    });
  }

  goToDashboard(): void { this.router.navigate(['/dashboard']); }
  goToPricing():   void { this.router.navigate(['/pricing']); }
}
