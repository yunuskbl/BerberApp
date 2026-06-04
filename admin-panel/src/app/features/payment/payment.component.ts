import { Component, OnInit, AfterViewInit, SecurityContext } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { environment } from '../../../environments/environment';

@Component({
  selector: 'app-payment',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './payment.component.html',
  styleUrl: './payment.component.scss'
})
export class PaymentComponent implements OnInit, AfterViewInit {
  plan = '';
  planLabel = '';
  price = 0;

  state: 'loading' | 'form' | 'success' | 'fail' = 'loading';
  errorMessage = '';
  checkoutHtml: SafeHtml = '';
  successPlan = '';
  failReason = '';

  readonly planMap: Record<string, { label: string; price: number; icon: string }> = {
    baslangic:   { label: 'Başlangıç',   price: 899,  icon: '🌱' },
    profesyonel: { label: 'Profesyonel', price: 1799, icon: '⚡' },
    premium:     { label: 'Premium',     price: 2399, icon: '👑' },
  };

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private http: HttpClient,
    private sanitizer: DomSanitizer
  ) {}

  ngOnInit(): void {
    this.route.queryParamMap.subscribe(params => {
      const status = params.get('status');
      const plan   = params.get('plan');
      const reason = params.get('reason');

      if (status === 'success') {
        this.successPlan = this.planMap[plan ?? '']?.label ?? plan ?? '';
        this.state = 'success';
        return;
      }
      if (status === 'fail') {
        this.failReason = reason ?? 'Ödeme işlemi tamamlanamadı.';
        this.state = 'fail';
        return;
      }

      this.plan = params.get('plan') ?? '';
      const info = this.planMap[this.plan];
      if (!info) { this.router.navigate(['/pricing']); return; }
      this.planLabel = info.label;
      this.price     = info.price;
      this.initiatePayment();
    });
  }

  ngAfterViewInit(): void {}

  initiatePayment(): void {
    this.state = 'loading';
    this.http.post<any>(`${environment.apiUrl}/subscription/checkout`, { plan: this.plan }).subscribe({
      next: res => {
        if (res.success) {
          this.checkoutHtml = this.sanitizer.bypassSecurityTrustHtml(res.data.checkoutFormContent);
          this.state = 'form';
          setTimeout(() => this.injectScript(), 100);
        } else {
          this.errorMessage = res.message ?? 'Ödeme formu yüklenemedi.';
          this.state = 'fail';
        }
      },
      error: () => {
        this.errorMessage = 'Sunucu hatası. Lütfen tekrar deneyin.';
        this.state = 'fail';
      }
    });
  }

  private injectScript(): void {
    const container = document.getElementById('iyzico-checkout-form');
    if (!container) return;
    const scripts = container.querySelectorAll('script');
    scripts.forEach(oldScript => {
      const newScript = document.createElement('script');
      Array.from(oldScript.attributes).forEach(attr =>
        newScript.setAttribute(attr.name, attr.value));
      newScript.textContent = oldScript.textContent;
      oldScript.parentNode?.replaceChild(newScript, oldScript);
    });
  }

  goToDashboard(): void { this.router.navigate(['/dashboard']); }
  goToPricing():   void { this.router.navigate(['/pricing']); }
  retry():         void { this.initiatePayment(); }
}
