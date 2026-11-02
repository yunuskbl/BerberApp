import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { SubscriptionService } from '../../core/services/subscription.service';
import { PLANS, formatPrice } from '../../core/pricing';

@Component({
  selector: 'app-checkout',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './checkout.component.html',
  styleUrl: './checkout.component.scss'
})
export class CheckoutComponent implements OnInit {
  plan = '';
  isLoading = true;
  error = '';
  formContent: SafeHtml | null = null;

  /** Eski plan adları (Basic/Standard/Full) yeni anahtarlara; fiyat tek kaynaktan. */
  private readonly legacyKeys: Record<string, string> = {
    Basic: 'baslangic', Standard: 'profesyonel', Full: 'premium',
    Baslangic: 'baslangic', Profesyonel: 'profesyonel', Premium: 'premium',
  };

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private sanitizer: DomSanitizer,
    private subscriptionService: SubscriptionService
  ) {}

  ngOnInit() {
    this.plan = this.route.snapshot.queryParamMap.get('plan') ?? '';
    if (!this.plan || this.plan === 'Basic') {
      this.router.navigate(['/pricing']);
      return;
    }
    this.loadCheckoutForm();
  }

  loadCheckoutForm() {
    this.isLoading = true;
    this.error = '';
    this.subscriptionService.initCheckout(this.plan).subscribe({
      next: (res) => {
        if (res.success && res.data?.checkoutFormContent) {
          this.formContent = this.sanitizer.bypassSecurityTrustHtml(res.data.checkoutFormContent);
          this.isLoading = false;
          setTimeout(() => this.injectScript(res.data.checkoutFormContent), 100);
        } else {
          this.error = 'Ödeme formu yüklenemedi.';
          this.isLoading = false;
        }
      },
      error: () => {
        this.error = 'Sunucu hatası. Lütfen tekrar deneyin.';
        this.isLoading = false;
      }
    });
  }

  private injectScript(html: string) {
    const scriptMatch = html.match(/src="([^"]+iyzipay[^"]+)"/i);
    if (!scriptMatch) return;
    const existing = document.querySelector(`script[src="${scriptMatch[1]}"]`);
    if (existing) return;
    const s = document.createElement('script');
    s.src = scriptMatch[1];
    document.body.appendChild(s);
  }

  get price(): number { return PLANS[this.legacyKeys[this.plan] ?? '']?.monthly ?? 0; }

  fmt(value: number): string { return formatPrice(value); }
}
