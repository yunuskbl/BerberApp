import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { PLANS, annualSaving, durationDays, formatPrice as formatTry, priceFor } from '../../core/pricing';
import { LanguageService } from '../../core/services/language.service';

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
  /**
   * Tutarları Angular'ın `number` pipe'ı yerine Intl ile biçimlendiriyoruz:
   * pipe varsayılan en-US yerelini kullanıyor ve ₺2.399 "₺2,399" olarak
   * çıkıyordu — Türkçe okurda bu "iki virgül dokuz" demek. Raporlar sayfası da
   * aynı yolu izliyor.
   */
  formatPrice(value: number): string {
    return formatTry(value, this.langService.dateLocale);
  }

  get price(): number        { return priceFor(this.monthlyPrice, this.billingPeriod); }
  get durationDays(): number { return durationDays(this.billingPeriod); }
  get annualPrice(): number  { return priceFor(this.monthlyPrice, 'annual'); }
  get saving(): number       { return annualSaving(this.monthlyPrice); }

  state: 'loading' | 'select-billing' | 'bank-info' | 'submitting' | 'submitted' | 'fail' = 'loading';
  errorMessage = '';
  referenceCode = '';

  /** Fiyatlar core/pricing.ts'te; burada ayrı bir kopya tutmak Premium'un
      iki farklı tutarla dolaşmasına yol açmıştı. */
  readonly planMap = PLANS;

  readonly bankInfo = {
    bankName:      'Garanti Bankası',
    iban:          'TR97 0006 2000 5770 0006 6537 82',
    accountHolder: 'Yunus Emre Kobal',
  };

  constructor(
    public langService: LanguageService,
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
      this.monthlyPrice = info.monthly;

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
