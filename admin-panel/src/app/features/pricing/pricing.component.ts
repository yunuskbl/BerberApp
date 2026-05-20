import { Component, AfterViewInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { trigger, transition, style, animate } from '@angular/animations';

interface Feature  { name: string; included: boolean; }
interface PlanLimit { staff: string; appointments: string; }
interface Plan {
  name: string; label: string; price: number; description: string;
  icon: string; featured: boolean; features: Feature[]; cta: string; ctaDisabled?: boolean;
  limits: PlanLimit;
}
interface FAQ  { question: string; answer: string; open?: boolean; }
interface Stat { value: string; label: string; num: number; format: (n: number) => string; }

const ALL_FEATURES: Feature[] = [
  { name: 'Online Randevu Sayfası',        included: true },
  { name: 'Randevu Yönetimi',              included: true },
  { name: 'Çoklu Hizmet Seçimi',           included: true },
  { name: 'Müşteri & Personel Yönetimi',   included: true },
  { name: 'Personele Özel Fiyatlandırma',  included: true },
  { name: 'Hizmet & Fiyat Yönetimi',       included: true },
  { name: 'Tema & Logo Özelleştirme',      included: true },
  { name: 'Çoklu Dil (TR / EN / RU)',      included: true },
  { name: 'WhatsApp Bildirimleri',          included: true },
  { name: 'Randevu Hatırlatması',           included: true },
  { name: 'Fotoğraf Galerisi',             included: true },
  { name: 'Müşteri Değerlendirme Sistemi', included: true },
  { name: 'Rapor & Gelir Analizi',         included: true },
  { name: 'Öncelikli Destek',              included: true },
];

@Component({
  selector: 'app-pricing',
  standalone: true,
  imports: [CommonModule, RouterModule],
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
export class PricingComponent implements AfterViewInit, OnDestroy {

  // ── Billing toggle ────────────────────────────────────────────────────────
  billingYearly  = false;
  priceAnimating = false;

  // ── Stats counter ─────────────────────────────────────────────────────────
  stats: Stat[] = [
    { value: '500+', label: 'Aktif İşletme',      num: 500, format: n => `${n}+`   },
    { value: '50K+', label: 'Aylık Randevu',       num: 50,  format: n => `${n}K+`  },
    { value: '%98',  label: 'Müşteri Memnuniyeti', num: 98,  format: n => `%${n}`   },
    { value: '3 dk', label: 'Kurulum Süresi',      num: 3,   format: n => `${n} dk` },
  ];
  animatedStats = this.stats.map(s => s.format(0)); // ['0+', '0K+', '%0', '0 dk']

  // ── Card entry animation ──────────────────────────────────────────────────
  cardVisible: boolean[] = [false, false, false];

  private observers: IntersectionObserver[] = [];

  // ── Feature list ──────────────────────────────────────────────────────────
  appFeatures = [
    {
      icon: `<svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.75" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" d="M6.75 3v2.25M17.25 3v2.25M3 18.75V7.5a2.25 2.25 0 0 1 2.25-2.25h13.5A2.25 2.25 0 0 1 21 7.5v11.25m-18 0A2.25 2.25 0 0 0 5.25 21h13.5A2.25 2.25 0 0 0 21 18.75m-18 0v-7.5A2.25 2.25 0 0 1 5.25 9h13.5A2.25 2.25 0 0 1 21 11.25v7.5"/></svg>`,
      title: 'Online Randevu Sayfası',
      desc: 'Müşterileriniz 7/24 linkten randevu alır. Telefon trafiği sıfıra iner, doluluk artar.'
    },
    {
      icon: `<svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.75" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" d="M8.25 6.75h7.5M8.25 9.75h4.5m-7.5 9a2.25 2.25 0 0 1-2.25-2.25V5.25A2.25 2.25 0 0 1 5.25 3h13.5A2.25 2.25 0 0 1 21 5.25v11.25A2.25 2.25 0 0 1 18.75 18.75H12l-4.5 4.5v-4.5H5.25Z"/></svg>`,
      title: 'Çoklu Hizmet Seçimi',
      desc: 'Müşteriler tek randevuda birden fazla hizmet seçebilir. Toplam süre ve ücret otomatik hesaplanır.'
    },
    {
      icon: `<svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.75" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" d="M7.5 8.25h9m-9 3H12m-9.75 1.51c0 1.6 1.123 2.994 2.707 3.227 1.129.166 2.27.293 3.423.379.35.026.67.21.865.501L12 21l2.755-4.133a1.14 1.14 0 0 1 .865-.501 48.172 48.172 0 0 0 3.423-.379c1.584-.233 2.707-1.626 2.707-3.228V6.741c0-1.602-1.123-2.995-2.707-3.228A48.394 48.394 0 0 0 12 3c-2.392 0-4.744.175-7.043.513C3.373 3.746 2.25 5.14 2.25 6.741v6.018Z"/></svg>`,
      title: 'WhatsApp Bildirimleri',
      desc: 'Randevu onayı, hatırlatma ve iptal mesajları müşterinize otomatik WhatsApp\'tan iletilir. Ek kurulum gerekmez.'
    },
    {
      icon: `<svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.75" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" d="M11.48 3.499a.562.562 0 0 1 1.04 0l2.125 5.111a.563.563 0 0 0 .475.345l5.518.442c.499.04.701.663.321.988l-4.204 3.602a.563.563 0 0 0-.182.557l1.285 5.385a.562.562 0 0 1-.84.61l-4.725-2.885a.562.562 0 0 0-.586 0L6.982 20.54a.562.562 0 0 1-.84-.61l1.285-5.386a.562.562 0 0 0-.182-.557l-4.204-3.602a.562.562 0 0 1 .321-.988l5.518-.442a.563.563 0 0 0 .475-.345L11.48 3.5Z"/></svg>`,
      title: 'Değerlendirme Sistemi',
      desc: 'Hizmet tamamlandığında müşteriye otomatik puan linki gönderilir. Yorumlar ve puanlar profil sayfanızda yayınlanır.'
    },
    {
      icon: `<svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.75" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" d="M3 13.125C3 12.504 3.504 12 4.125 12h2.25c.621 0 1.125.504 1.125 1.125v6.75C7.5 20.496 6.996 21 6.375 21h-2.25A1.125 1.125 0 0 1 3 19.875v-6.75ZM9.75 8.625c0-.621.504-1.125 1.125-1.125h2.25c.621 0 1.125.504 1.125 1.125v11.25c0 .621-.504 1.125-1.125 1.125h-2.25a1.125 1.125 0 0 1-1.125-1.125V8.625ZM16.5 4.125c0-.621.504-1.125 1.125-1.125h2.25C20.496 3 21 3.504 21 4.125v15.75c0 .621-.504 1.125-1.125 1.125h-2.25a1.125 1.125 0 0 1-1.125-1.125V4.125Z"/></svg>`,
      title: 'Gelir & Rapor Analizi',
      desc: 'Aylık gelir, doluluk oranı ve personel bazlı performans raporlarına tek ekrandan ulaşın.'
    },
    {
      icon: `<svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.75" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" d="M15 19.128a9.38 9.38 0 0 0 2.625.372 9.337 9.337 0 0 0 4.121-.952 4.125 4.125 0 0 0-7.533-2.493M15 19.128v-.003c0-1.113-.285-2.16-.786-3.07M15 19.128v.106A12.318 12.318 0 0 1 8.624 21c-2.331 0-4.512-.645-6.374-1.766l-.001-.109a6.375 6.375 0 0 1 11.964-3.07M12 6.375a3.375 3.375 0 1 1-6.75 0 3.375 3.375 0 0 1 6.75 0Zm8.25 2.25a2.625 2.625 0 1 1-5.25 0 2.625 2.625 0 0 1 5.25 0Z"/></svg>`,
      title: 'Personel & Fiyat Yönetimi',
      desc: 'Her personele özel hizmet fiyatı tanımlayın. Çalışma saatlerini ve izinleri kolayca yönetin.'
    },
  ];

  plans: Plan[] = [
    {
      name: 'baslangic', label: 'Başlangıç', price: 899,
      description: 'Tek kişilik işletmeler için tam araçlar',
      icon: '🌱', featured: false, ctaDisabled: false,
      limits: { staff: '1 Personel', appointments: '100 Randevu/Ay' },
      features: [
        { name: 'Online Randevu Sayfası',        included: true  },
        { name: 'Randevu Yönetimi',              included: true  },
        { name: 'Çoklu Hizmet Seçimi',           included: true  },
        { name: 'Müşteri & Personel Yönetimi',   included: true  },
        { name: 'Personele Özel Fiyatlandırma',  included: true  },
        { name: 'Hizmet & Fiyat Yönetimi',       included: true  },
        { name: 'Tema & Logo Özelleştirme',      included: true  },
        { name: 'Çoklu Dil (TR / EN / RU)',      included: true  },
        { name: 'WhatsApp Bildirimleri',          included: true  },
        { name: 'Randevu Hatırlatması',           included: true  },
        { name: 'Fotoğraf Galerisi',             included: true  },
        { name: 'Müşteri Değerlendirme Sistemi', included: true  },
        { name: 'Rapor & Gelir Analizi',         included: false },
        { name: 'Öncelikli Destek',              included: false },
      ],
      cta: 'Başlangıç Planı',
    },
    {
      name: 'profesyonel', label: 'Profesyonel', price: 1799,
      description: 'Büyüyen işletmeler için tam donanım',
      icon: '⚡', featured: true,
      limits: { staff: '5 Personele Kadar', appointments: '500 Randevu/Ay' },
      features: [
        { name: 'Online Randevu Sayfası',        included: true  },
        { name: 'Randevu Yönetimi',              included: true  },
        { name: 'Çoklu Hizmet Seçimi',           included: true  },
        { name: 'Müşteri & Personel Yönetimi',   included: true  },
        { name: 'Personele Özel Fiyatlandırma',  included: true  },
        { name: 'Hizmet & Fiyat Yönetimi',       included: true  },
        { name: 'Tema & Logo Özelleştirme',      included: true  },
        { name: 'Çoklu Dil (TR / EN / RU)',      included: true  },
        { name: 'WhatsApp Bildirimleri',          included: true  },
        { name: 'Randevu Hatırlatması',           included: true  },
        { name: 'Fotoğraf Galerisi',             included: true  },
        { name: 'Müşteri Değerlendirme Sistemi', included: true  },
        { name: 'Rapor & Gelir Analizi',         included: true  },
        { name: 'Öncelikli Destek',              included: false },
      ],
      cta: 'Profesyonel\'e Geç',
    },
    {
      name: 'premium', label: 'Premium', price: 2399,
      description: 'Büyük salonlar için sınırsız kullanım',
      icon: '👑', featured: false,
      limits: { staff: 'Sınırsız Personel', appointments: 'Sınırsız Randevu' },
      features: ALL_FEATURES,
      cta: 'Premium\'a Geç',
    },
  ];

  allFeatureNames = ALL_FEATURES.map(f => f.name);

  faqItems: FAQ[] = [
    {
      question: 'Hangi tür işletmeler kullanabilir?',
      answer: 'Randevu alan her tür hizmet işletmesi için uygundur. Berber, kuaför, güzellik salonu, masaj merkezi, dişçi, klinik, dövme stüdyosu, fotoğrafçı, danışmanlık ofisi ve daha fazlası. Hizmetlerinizi ve çalışma saatlerinizi sisteme girmeniz yeterli.',
    },
    {
      question: 'Kayıt olmak için ne gerekiyor?',
      answer: 'Sadece bir e-posta adresi ve telefon numarası yeterli. 3 dakikada sisteme kayıt olup randevu almaya başlayabilirsiniz.',
    },
    {
      question: 'İstediğim zaman plan değiştirebilir miyim?',
      answer: 'Evet! İstediğiniz zaman planınızı yükseltebilirsiniz. Değişiklik anında geçerli olur, ek ücret hesaplanmaz.',
    },
    {
      question: 'WhatsApp bildirimleri nasıl çalışıyor?',
      answer: 'Randevu onayı, iptal ve hatırlatma mesajları müşterinizin telefonuna otomatik olarak WhatsApp üzerinden gönderilir. Ek bir kurulum gerekmez.',
    },
    {
      question: 'Müşteri birden fazla hizmet alabilir mi?',
      answer: 'Evet. Müşteriler randevu alırken birden fazla hizmet seçebilir. Sistem toplam süreyi ve ücreti otomatik hesaplar, uygun zaman dilimlerini buna göre gösterir.',
    },
    {
      question: 'Personel ve randevu limitleri nedir?',
      answer: 'Başlangıç planında 1 personel ve ayda 100 randevu, Profesyonel planında 5 personele kadar ve ayda 500 randevu desteklenir. Premium planda her ikisi de sınırsızdır.',
    },
    {
      question: 'Verilerim güvende mi?',
      answer: 'Tüm verileriniz şifreli olarak Türkiye\'deki sunucularımızda saklanır ve düzenli olarak yedeklenir.',
    },
  ];

  // ── Lifecycle ─────────────────────────────────────────────────────────────
  get isLoggedIn(): boolean { return !!localStorage.getItem('accessToken'); }

  constructor(private router: Router) {}

  ngAfterViewInit(): void {
    // Stats counter: fires when stats section scrolls into view
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

    // Card visibility: staggered entrance per card
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

  // ── Counter animation ─────────────────────────────────────────────────────
  animateCounter(index: number, target: number, format: (n: number) => string): void {
    const STEPS = 60;
    let step = 0;
    const timer = setInterval(() => {
      step++;
      const t      = step / STEPS;
      const eased  = 1 - Math.pow(1 - t, 3); // ease-out cubic
      this.animatedStats[index] = format(Math.round(eased * target));
      if (step >= STEPS) clearInterval(timer);
    }, 33); // 60 × 33ms ≈ 2 s
  }

  // ── Billing ───────────────────────────────────────────────────────────────
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

  // ── 3-D card tilt ─────────────────────────────────────────────────────────
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

  // ── Navigation ────────────────────────────────────────────────────────────
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

  planHas(plan: Plan, featureName: string): boolean {
    return plan.features.find(f => f.name === featureName)?.included ?? false;
  }
}
