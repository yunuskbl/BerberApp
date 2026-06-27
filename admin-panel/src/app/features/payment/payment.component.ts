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
  monthlyPrice = 0;

  // Seçilen faturalama periyodu
  billingPeriod: 'monthly' | 'annual' = 'monthly';
  get price(): number {
    return this.billingPeriod === 'annual'
      ? Math.round(this.monthlyPrice * 12 * 0.8)
      : this.monthlyPrice;
  }
  get durationDays(): number { return this.billingPeriod === 'annual' ? 365 : 30; }
  get annualPrice(): number  { return Math.round(this.monthlyPrice * 12 * 0.8); }
  get saving(): number       { return Math.round(this.monthlyPrice * 12 * 0.2); }

  state: 'loading' | 'select-billing' | 'bank-info' | 'submitting' | 'submitted' | 'fail' = 'loading';
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
      this.planLabel    = info.label;
      this.monthlyPrice = info.price;

      // Pricing sayfasından yıllık seçili geldiyse
      if (params.get('billing') === 'yearly') {
        this.billingPeriod = 'annual';
      }

      this.state = 'select-billing';
    });
  }

  selectBilling(period: 'monthly' | 'annual'): void {
    this.billingPeriod = period;
    this.state = 'bank-info';
  }

  confirmTransfer(): void {
    this.state = 'submitting';
    const periodLabel = this.billingPeriod === 'annual' ? 'Yıllık' : 'Aylık';
    this.http.post<any>(`${environment.apiUrl}/payment-request/submit`, {
      planName:     this.plan,
      planLabel:    `${this.planLabel} (${periodLabel})`,
      amount:       this.price,
      durationDays: this.durationDays
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

  goBack(): void        { this.state = 'select-billing'; }
  goToDashboard(): void { this.router.navigate(['/dashboard']); }
  goToPricing():   void { this.router.navigate(['/pricing']); }
}
