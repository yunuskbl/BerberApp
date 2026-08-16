import { Component, AfterViewInit, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { TranslatePipe } from '../../shared/pipes/translate.pipe';
import { LanguageService, Lang } from '../../core/services/language.service';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { trigger, transition, style, animate } from '@angular/animations';

interface Feature  { name: string; included: boolean; }
interface PlanLimit { staff: string; appointments: string; }
interface Plan {
  name: string; labelKey: string; price: number; descKey: string;
  icon: string; featured: boolean; features: Feature[]; ctaKey: string; ctaDisabled?: boolean;
  limits: PlanLimit;
}
interface FAQ  { questionKey: string; answerKey: string; open?: boolean; }
interface Stat { value: string; labelKey: string; num: number; format: (n: number) => string; }

// Internal feature keys — shared between ALL_FEATURES and plan feature lists
const F = {
  onlineBooking:   'pricing.f.onlineBooking',
  calendar:        'pricing.f.calendar',
  multiService:    'pricing.f.multiService',
  whatsappOtp:     'pricing.f.whatsappOtp',
  intlPhone:       'pricing.f.intlPhone',
  qrCode:          'pricing.f.qrCode',
  map:             'pricing.f.map',
  hours:           'pricing.f.hours',
  staffPhoto:      'pricing.f.staffPhoto',
  customerStaff:   'pricing.f.customerStaff',
  staffPricing:    'pricing.f.staffPricing',
  servicePrice:    'pricing.f.servicePrice',
  theme:           'pricing.f.theme',
  multiLang:       'pricing.f.multiLang',
  waNotif:         'pricing.f.waNotif',
  reminder:        'pricing.f.reminder',
  gallery:         'pricing.f.gallery',
  reviews:         'pricing.f.reviews',
  manualApproval:  'pricing.f.manualApproval',
  waCampaign:      'pricing.f.waCampaign',
  finance:         'pricing.f.finance',
  staffLogin:      'pricing.f.staffLogin',
  multiBranch:     'pricing.f.multiBranch',
  prioritySupport: 'pricing.f.prioritySupport',
};

const ALL_FEATURE_KEYS: Feature[] = Object.values(F).map(k => ({ name: k, included: true }));

@Component({
  selector: 'app-pricing',
  standalone: true,
  imports: [CommonModule, RouterModule, TranslatePipe],
  templateUrl: './pricing.component.html',
  styleUrls: ['./pricing.component.scss'],
  animations: [
    trigger('fadeIn', [
      transition(':enter', [
        style({ opacity: 0, transform: 'translateY(12px)' }),
        animate('400ms ease-out', style({ opacity: 1, transform: 'translateY(0)' }))
      ])
    ])
  ]
})
export class PricingComponent implements OnInit, AfterViewInit, OnDestroy {

  isUpgradeMode = false;
  billingYearly  = false;
  priceAnimating = false;

  stats: Stat[] = [
    { value: '200+', labelKey: 'pricing.stat.businesses',   num: 200, format: n => `${n}+`   },
    { value: '15K+', labelKey: 'pricing.stat.appointments', num: 15,  format: n => `${n}K+`  },
    { value: '%98',  labelKey: 'pricing.stat.satisfaction', num: 98,  format: n => `%${n}`   },
    { value: '3 dk', labelKey: 'pricing.stat.setupTime',    num: 3,   format: n => `${n} dk` },
  ];
  animatedStats = this.stats.map(s => s.format(0));

  cardVisible: boolean[] = [false, false, false, false, false, false, false];

  private observers: IntersectionObserver[] = [];

  private rawFeatureKeys = [
    { cardKey: 'booking',      icon: `<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><rect x="3" y="4" width="18" height="18" rx="2"/><line x1="16" y1="2" x2="16" y2="6"/><line x1="8" y1="2" x2="8" y2="6"/><line x1="3" y1="10" x2="21" y2="10"/><path d="M8 14h.01M12 14h.01M16 14h.01M8 18h.01M12 18h.01"/></svg>` },
    { cardKey: 'multiService', icon: `<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M9 11l3 3L22 4"/><path d="M21 12v7a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h11"/></svg>` },
    { cardKey: 'waNotif',      icon: `<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M21 15a2 2 0 0 1-2 2H7l-4 4V5a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2z"/><line x1="9" y1="10" x2="15" y2="10"/><line x1="9" y1="14" x2="13" y2="14"/></svg>` },
    { cardKey: 'reviews',      icon: `<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><polygon points="12 2 15.09 8.26 22 9.27 17 14.14 18.18 21.02 12 17.77 5.82 21.02 7 14.14 2 9.27 8.91 8.26 12 2"/></svg>` },
    { cardKey: 'finance',      icon: `<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><line x1="18" y1="20" x2="18" y2="10"/><line x1="12" y1="20" x2="12" y2="4"/><line x1="6" y1="20" x2="6" y2="14"/><line x1="2" y1="20" x2="22" y2="20"/></svg>` },
    { cardKey: 'staffPrice',   icon: `<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2"/><circle cx="9" cy="7" r="4"/><path d="M23 21v-2a4 4 0 0 0-3-3.87"/><path d="M16 3.13a4 4 0 0 1 0 7.75"/></svg>` },
    { cardKey: 'waCampaign',   icon: `<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M22 16.92v3a2 2 0 0 1-2.18 2 19.79 19.79 0 0 1-8.63-3.07A19.5 19.5 0 0 1 4.69 12 19.79 19.79 0 0 1 1.65 3.21a2 2 0 0 1 1.99-2.18h3a2 2 0 0 1 2 1.72c.127.96.361 1.903.7 2.81a2 2 0 0 1-.45 2.11L8.09 9.91a16 16 0 0 0 6 6l1.27-1.27a2 2 0 0 1 2.11-.45c.907.339 1.85.573 2.81.7A2 2 0 0 1 22 16.92z"/></svg>` },
    { cardKey: 'staffLogin',   icon: `<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><rect x="3" y="11" width="18" height="11" rx="2" ry="2"/><path d="M7 11V7a5 5 0 0 1 10 0v4"/><circle cx="12" cy="16" r="1" fill="currentColor"/></svg>` },
  ];

  appFeatures: Array<{ icon: SafeHtml; titleKey: string; descKey: string }> = [];

  plans: Plan[] = [
    {
      name: 'baslangic', labelKey: 'pricing.plan.baslangic.label', price: 899,
      descKey: 'pricing.plan.baslangic.desc',
      icon: '🌱', featured: false, ctaDisabled: false,
      limits: { staff: 'pricing.plan.baslangic.staff', appointments: 'pricing.plan.baslangic.appts' },
      features: [
        { name: F.onlineBooking,  included: true  },
        { name: F.calendar,       included: true  },
        { name: F.multiService,   included: true  },
        { name: F.whatsappOtp,    included: true  },
        { name: F.intlPhone,      included: true  },
        { name: F.qrCode,         included: true  },
        { name: F.map,            included: true  },
        { name: F.hours,          included: true  },
        { name: F.staffPhoto,     included: true  },
        { name: F.customerStaff,  included: true  },
        { name: F.staffPricing,   included: true  },
        { name: F.servicePrice,   included: true  },
        { name: F.theme,          included: true  },
        { name: F.multiLang,      included: true  },
        { name: F.waNotif,        included: true  },
        { name: F.reminder,       included: true  },
        { name: F.gallery,        included: true  },
        { name: F.reviews,        included: true  },
        { name: F.manualApproval, included: true  },
        { name: F.waCampaign,     included: false },
        { name: F.finance,        included: false },
        { name: F.staffLogin,     included: false },
        { name: F.multiBranch,    included: false },
        { name: F.prioritySupport,included: false },
      ],
      ctaKey: 'pricing.plan.baslangic.cta',
    },
    {
      name: 'profesyonel', labelKey: 'pricing.plan.profesyonel.label', price: 1799, descKey: 'pricing.plan.profesyonel.desc',
      icon: '⚡', featured: true,
      limits: { staff: 'pricing.plan.profesyonel.staff', appointments: 'pricing.plan.profesyonel.appts' },
      features: [
        { name: F.onlineBooking,  included: true  },
        { name: F.calendar,       included: true  },
        { name: F.multiService,   included: true  },
        { name: F.whatsappOtp,    included: true  },
        { name: F.intlPhone,      included: true  },
        { name: F.qrCode,         included: true  },
        { name: F.map,            included: true  },
        { name: F.hours,          included: true  },
        { name: F.staffPhoto,     included: true  },
        { name: F.customerStaff,  included: true  },
        { name: F.staffPricing,   included: true  },
        { name: F.servicePrice,   included: true  },
        { name: F.theme,          included: true  },
        { name: F.multiLang,      included: true  },
        { name: F.waNotif,        included: true  },
        { name: F.reminder,       included: true  },
        { name: F.gallery,        included: true  },
        { name: F.reviews,        included: true  },
        { name: F.manualApproval, included: true  },
        { name: F.waCampaign,     included: true  },
        { name: F.finance,        included: true  },
        { name: F.staffLogin,     included: true  },
        { name: F.multiBranch,    included: false },
        { name: F.prioritySupport,included: false },
      ],
      ctaKey: 'pricing.plan.profesyonel.cta',
    },
    {
      name: 'premium', labelKey: 'pricing.plan.premium.label', price: 2999,
      descKey: 'pricing.plan.premium.desc',
      icon: '👑', featured: false,
      limits: { staff: 'pricing.plan.premium.staff', appointments: 'pricing.plan.premium.appts' },
      features: ALL_FEATURE_KEYS,
      ctaKey: 'pricing.plan.premium.cta',
    },
  ];

  allFeatureKeys = Object.values(F);

  faqItems: FAQ[] = Array.from({ length: 13 }, (_, i) => ({
    questionKey: `pricing.faq.${i}.q`,
    answerKey:   `pricing.faq.${i}.a`,
    open: false,
  }));

  langs: Lang[] = ['tr', 'en', 'ru', 'de'];

  get isLoggedIn(): boolean { return !!localStorage.getItem('accessToken'); }

  constructor(
    private router: Router,
    private sanitizer: DomSanitizer,
    private route: ActivatedRoute,
    public langSvc: LanguageService,
  ) {
    this.appFeatures = this.rawFeatureKeys.map(f => ({
      icon:     this.sanitizer.bypassSecurityTrustHtml(f.icon),
      titleKey: `pricing.card.${f.cardKey}.title`,
      descKey:  `pricing.card.${f.cardKey}.desc`,
    }));
  }

  ngOnInit(): void {
    this.route.queryParamMap.subscribe(params => {
      this.isUpgradeMode = params.get('upgrade') === '1';
    });
  }

  ngAfterViewInit(): void {
    if (this.isUpgradeMode) {
      setTimeout(() => document.getElementById('pricing')?.scrollIntoView({ behavior: 'smooth' }), 100);
    }
    const statsSection = document.querySelector('.stats-section');
    if (statsSection) {
      const statsObs = new IntersectionObserver(entries => {
        if (entries[0].isIntersecting) {
          this.stats.forEach((s, i) => this.animateCounter(i, s.num, s.format));
          statsObs.disconnect();
        }
      }, { threshold: 0.5 });
      statsObs.observe(statsSection);
      this.observers.push(statsObs);
    }

    const wrappers = document.querySelectorAll('.plan-card-wrapper');
    const cardObs = new IntersectionObserver(entries => {
      entries.forEach(entry => {
        if (entry.isIntersecting) {
          const idx = +(entry.target as HTMLElement).dataset['index']!;
          setTimeout(() => { this.cardVisible[idx] = true; }, idx * 150);
          cardObs.unobserve(entry.target);
        }
      });
    }, { threshold: 0.12 });
    wrappers.forEach(w => cardObs.observe(w));
    this.observers.push(cardObs);
  }

  ngOnDestroy(): void {
    this.observers.forEach(o => o.disconnect());
  }

  animateCounter(index: number, target: number, format: (n: number) => string): void {
    const STEPS = 60;
    let step = 0;
    const timer = setInterval(() => {
      step++;
      const t      = step / STEPS;
      const eased  = 1 - Math.pow(1 - t, 3);
      this.animatedStats[index] = format(Math.round(eased * target));
      if (step >= STEPS) clearInterval(timer);
    }, 33);
  }

  getDisplayPrice(basePrice: number): number {
    return this.billingYearly ? Math.round(basePrice * 0.8) : basePrice;
  }

  toggleBilling(): void {
    this.priceAnimating = true;
    setTimeout(() => {
      this.billingYearly  = !this.billingYearly;
      this.priceAnimating = false;
    }, 150);
  }

  onCardMouseMove(event: MouseEvent): void {
    const card = event.currentTarget as HTMLElement;
    card.style.transition = 'box-shadow 0.15s';
    const rect   = card.getBoundingClientRect();
    const x      = (event.clientX - rect.left) / rect.width  - 0.5;
    const y      = (event.clientY - rect.top)  / rect.height - 0.5;
    const baseY  = card.classList.contains('featured') ? -8 : 0;
    card.style.transform  = `perspective(1000px) rotateX(${-y * 10}deg) rotateY(${x * 10}deg) translateY(${baseY}px)`;
    card.style.boxShadow  = card.classList.contains('featured')
      ? '0 24px 64px rgba(124,58,237,.4)'
      : '0 18px 50px rgba(124,58,237,.18)';
  }

  onCardMouseLeave(event: MouseEvent): void {
    const card = event.currentTarget as HTMLElement;
    card.style.transition = 'transform 0.5s ease, box-shadow 0.4s';
    card.style.transform  = '';
    card.style.boxShadow  = '';
  }

  goToApp(): void {
    this.router.navigate([this.isLoggedIn ? '/dashboard' : '/login']);
  }

  scrollTo(id: string): void {
    document.getElementById(id)?.scrollIntoView({ behavior: 'smooth' });
  }

  scrollToTop(): void {
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  goToRegister(): void {
    this.router.navigate(['/kayit']);
  }

  goToSalons(): void {
    window.open('/salons', '_blank');
  }

  selectPlan(name: string): void {
    const params: Record<string, string> = { plan: name };
    if (this.billingYearly) params['billing'] = 'yearly';
    if (this.isLoggedIn) {
      this.router.navigate(['/payment'], { queryParams: params });
    } else {
      this.router.navigate(['/kayit'], { queryParams: params });
    }
  }

  buyPlan(name: string): void {
    const params: Record<string, string> = { plan: name, mode: 'buy' };
    if (this.billingYearly) params['billing'] = 'yearly';
    if (this.isLoggedIn) {
      this.router.navigate(['/payment'], { queryParams: params });
    } else {
      this.router.navigate(['/kayit'], { queryParams: params });
    }
  }

  toggleFAQ(i: number): void { this.faqItems[i].open = !this.faqItems[i].open; }

  planHas(plan: Plan, featureKey: string): boolean {
    return plan.features.find(f => f.name === featureKey)?.included ?? false;
  }

  /** Kopyalama geri bildirimi — butonlarda "Kopyalandı ✓" göstermek için. */
  mailCopied = false;

  /**
   * mailto'yu yeni sekmede açmak, e-posta uygulaması tanımlı olmayan
   * kullanıcıda boş bir sekme bırakıyordu. Aynı sekmede yönlendiriyoruz
   * (uygulama yoksa hiçbir şey olmaz, boş sekme de kalmaz) ve adresi her
   * durumda panoya kopyalıyoruz ki kullanıcı eli boş kalmasın.
   */
  openMail(): void {
    navigator.clipboard?.writeText('info@ayarliyo.com').then(() => {
      this.mailCopied = true;
      setTimeout(() => (this.mailCopied = false), 2500);
    }).catch(() => {});
    window.location.href = 'mailto:info@ayarliyo.com?subject=Bilgi%20Talebi';
  }

  openWhatsApp(): void {
    window.open('https://wa.me/905305606916', '_blank');
  }

  setLang(l: Lang): void {
    this.langSvc.setLang(l);
  }
}
