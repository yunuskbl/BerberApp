import { Component, AfterViewInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { TranslatePipe } from '../../shared/pipes/translate.pipe';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
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
  { name: 'Online Randevu Sayfası',            included: true },
  { name: 'Randevu Yönetimi & Takvim',         included: true },
  { name: 'Çoklu Hizmet Seçimi',               included: true },
  { name: 'WhatsApp OTP Doğrulama',            included: true },
  { name: 'Uluslararası Telefon Desteği',      included: true },
  { name: 'QR Kod ile Randevu Linki',          included: true },
  { name: 'Harita & Konum Entegrasyonu',       included: true },
  { name: 'Çalışma Saati & İzin Yönetimi',    included: true },
  { name: 'Personel Profil Fotoğrafı',         included: true },
  { name: 'Müşteri & Personel Yönetimi',       included: true },
  { name: 'Personele Özel Fiyatlandırma',      included: true },
  { name: 'Hizmet & Fiyat Yönetimi',           included: true },
  { name: 'Tema & Logo Özelleştirme',          included: true },
  { name: 'Çoklu Dil (TR / EN / RU)',          included: true },
  { name: 'WhatsApp Bildirimleri',              included: true },
  { name: 'Hatırlatma (24 saat + 1 saat)',     included: true },
  { name: 'Fotoğraf Galerisi',                 included: true },
  { name: 'Müşteri Değerlendirme Sistemi',     included: true },
  { name: 'Manuel Randevu Onayı',              included: true },
  { name: 'Toplu WhatsApp Kampanyası',         included: true },
  { name: 'Gelir & Gider Yönetimi',            included: true },
  { name: 'Personel Girişi & Yetkilendirme',   included: true },
  { name: 'Çoklu Şube Yönetimi',               included: true },
  { name: 'Öncelikli Destek',                  included: true },
];

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
  cardVisible: boolean[] = [false, false, false, false, false, false, false];

  private observers: IntersectionObserver[] = [];

  // ── Feature list ──────────────────────────────────────────────────────────
  private rawFeatures = [
    {
      icon: `<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
        <rect x="3" y="4" width="18" height="18" rx="2"/>
        <line x1="16" y1="2" x2="16" y2="6"/>
        <line x1="8" y1="2" x2="8" y2="6"/>
        <line x1="3" y1="10" x2="21" y2="10"/>
        <path d="M8 14h.01M12 14h.01M16 14h.01M8 18h.01M12 18h.01"/>
      </svg>`,
      title: 'Online Randevu Sayfası',
      desc: 'Müşteriler 7/24 kendi linkinden randevu alır; personel, hizmet, tarih ve saat seçer. Telefon trafiği sıfıra iner, doluluk artar.'
    },
    {
      icon: `<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
        <path d="M9 11l3 3L22 4"/>
        <path d="M21 12v7a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h11"/>
      </svg>`,
      title: 'Çoklu Hizmet Seçimi',
      desc: 'Müşteriler tek randevuda birden fazla hizmet seçebilir. Toplam süre ve ücret otomatik hesaplanır, uygun saatler buna göre gösterilir.'
    },
    {
      icon: `<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
        <path d="M21 15a2 2 0 0 1-2 2H7l-4 4V5a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2z"/>
        <line x1="9" y1="10" x2="15" y2="10"/>
        <line x1="9" y1="14" x2="13" y2="14"/>
      </svg>`,
      title: 'WhatsApp Bildirimleri',
      desc: 'Randevu onayı, 24 saat ve 1 saat öncesi hatırlatma, iptal ve tamamlanma mesajları WhatsApp\'tan otomatik gider. Ek kurulum gerekmez.'
    },
    {
      icon: `<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
        <polygon points="12 2 15.09 8.26 22 9.27 17 14.14 18.18 21.02 12 17.77 5.82 21.02 7 14.14 2 9.27 8.91 8.26 12 2"/>
      </svg>`,
      title: 'Değerlendirme Sistemi',
      desc: 'Hizmet tamamlandığında müşteriye otomatik puan linki gönderilir. Yorumlar ve puanlar salon keşif sayfasında yayınlanır.'
    },
    {
      icon: `<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
        <line x1="18" y1="20" x2="18" y2="10"/>
        <line x1="12" y1="20" x2="12" y2="4"/>
        <line x1="6"  y1="20" x2="6"  y2="14"/>
        <line x1="2"  y1="20" x2="22" y2="20"/>
      </svg>`,
      title: 'Gelir & Gider Yönetimi',
      desc: 'Gelir, gider ve net kâr raporlarına tek ekrandan ulaşın. Kategori bazlı gider takibi ve tarih aralığı filtresiyle finansal durumunuzu anlık görün.'
    },
    {
      icon: `<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
        <path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2"/>
        <circle cx="9" cy="7" r="4"/>
        <path d="M23 21v-2a4 4 0 0 0-3-3.87"/>
        <path d="M16 3.13a4 4 0 0 1 0 7.75"/>
      </svg>`,
      title: 'Personel & Fiyat Yönetimi',
      desc: 'Her personele profil fotoğrafı, biyografi ve özel hizmet fiyatı tanımlayın. Çalışma saatleri, izin günleri ve salon kapanışlarını kolayca yönetin.'
    },
    {
      icon: `<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
        <path d="M22 16.92v3a2 2 0 0 1-2.18 2 19.79 19.79 0 0 1-8.63-3.07A19.5 19.5 0 0 1 4.69 12 19.79 19.79 0 0 1 1.65 3.21a2 2 0 0 1 1.99-2.18h3a2 2 0 0 1 2 1.72c.127.96.361 1.903.7 2.81a2 2 0 0 1-.45 2.11L8.09 9.91a16 16 0 0 0 6 6l1.27-1.27a2 2 0 0 1 2.11-.45c.907.339 1.85.573 2.81.7A2 2 0 0 1 22 16.92z"/>
      </svg>`,
      title: 'Toplu WhatsApp Kampanyası',
      desc: 'Tüm kayıtlı müşterilerinize tek tıkla kişiselleştirilmiş WhatsApp mesajı gönderin. Kampanya, indirim veya hatırlatma bildirimi için idealdir.'
    },
    {
      icon: `<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
        <rect x="3" y="11" width="18" height="11" rx="2" ry="2"/>
        <path d="M7 11V7a5 5 0 0 1 10 0v4"/>
        <circle cx="12" cy="16" r="1" fill="currentColor"/>
      </svg>`,
      title: 'Personel Girişi & Yetki',
      desc: 'Her personele ayrı giriş hesabı tanımlayın. Personel yalnızca kendi randevularını ve çalışma saatlerini görür; müşteri ve finans verileri admin\'de kalır.'
    },
  ];

  appFeatures: Array<{ icon: SafeHtml; title: string; desc: string }> = [];

  plans: Plan[] = [
    {
      name: 'baslangic', label: 'Başlangıç', price: 899,
      description: 'Tek lokasyonlu küçük işletmeler için',
      icon: '🌱', featured: false, ctaDisabled: false,
      limits: { staff: '1 Personel', appointments: '150 Randevu/Ay' },
      features: [
        { name: 'Online Randevu Sayfası',           included: true  },
        { name: 'Randevu Yönetimi & Takvim',        included: true  },
        { name: 'Çoklu Hizmet Seçimi',              included: true  },
        { name: 'WhatsApp OTP Doğrulama',           included: true  },
        { name: 'Uluslararası Telefon Desteği',     included: true  },
        { name: 'QR Kod ile Randevu Linki',         included: true  },
        { name: 'Harita & Konum Entegrasyonu',      included: true  },
        { name: 'Çalışma Saati & İzin Yönetimi',   included: true  },
        { name: 'Personel Profil Fotoğrafı',        included: true  },
        { name: 'Müşteri & Personel Yönetimi',      included: true  },
        { name: 'Personele Özel Fiyatlandırma',     included: true  },
        { name: 'Hizmet & Fiyat Yönetimi',          included: true  },
        { name: 'Tema & Logo Özelleştirme',         included: true  },
        { name: 'Çoklu Dil (TR / EN / RU)',         included: true  },
        { name: 'WhatsApp Bildirimleri',             included: true  },
        { name: 'Hatırlatma (24 saat + 1 saat)',    included: true  },
        { name: 'Fotoğraf Galerisi',                included: true  },
        { name: 'Müşteri Değerlendirme Sistemi',    included: true  },
        { name: 'Manuel Randevu Onayı',             included: true  },
        { name: 'Toplu WhatsApp Kampanyası',        included: false },
        { name: 'Gelir & Gider Yönetimi',           included: false },
        { name: 'Personel Girişi & Yetkilendirme',  included: false },
        { name: 'Çoklu Şube Yönetimi',              included: false },
        { name: 'Öncelikli Destek',                 included: false },
      ],
      cta: 'Başlangıç Planı',
    },
    {
      name: 'profesyonel', label: 'Profesyonel', price: 1799,
      description: 'Büyüyen tek şubeli işletmeler için',
      icon: '⚡', featured: true,
      limits: { staff: 'Sınırsız Personel', appointments: 'Sınırsız Randevu' },
      features: [
        { name: 'Online Randevu Sayfası',           included: true  },
        { name: 'Randevu Yönetimi & Takvim',        included: true  },
        { name: 'Çoklu Hizmet Seçimi',              included: true  },
        { name: 'WhatsApp OTP Doğrulama',           included: true  },
        { name: 'Uluslararası Telefon Desteği',     included: true  },
        { name: 'QR Kod ile Randevu Linki',         included: true  },
        { name: 'Harita & Konum Entegrasyonu',      included: true  },
        { name: 'Çalışma Saati & İzin Yönetimi',   included: true  },
        { name: 'Personel Profil Fotoğrafı',        included: true  },
        { name: 'Müşteri & Personel Yönetimi',      included: true  },
        { name: 'Personele Özel Fiyatlandırma',     included: true  },
        { name: 'Hizmet & Fiyat Yönetimi',          included: true  },
        { name: 'Tema & Logo Özelleştirme',         included: true  },
        { name: 'Çoklu Dil (TR / EN / RU)',         included: true  },
        { name: 'WhatsApp Bildirimleri',             included: true  },
        { name: 'Hatırlatma (24 saat + 1 saat)',    included: true  },
        { name: 'Fotoğraf Galerisi',                included: true  },
        { name: 'Müşteri Değerlendirme Sistemi',    included: true  },
        { name: 'Manuel Randevu Onayı',             included: true  },
        { name: 'Toplu WhatsApp Kampanyası',        included: true  },
        { name: 'Gelir & Gider Yönetimi',           included: true  },
        { name: 'Personel Girişi & Yetkilendirme',  included: true  },
        { name: 'Çoklu Şube Yönetimi',              included: false },
        { name: 'Öncelikli Destek',                 included: false },
      ],
      cta: 'Profesyonel\'e Geç',
    },
    {
      name: 'premium', label: 'Premium', price: 2399,
      description: 'Zincir işletmeler için merkezi yönetim',
      icon: '👑', featured: false,
      limits: { staff: 'Sınırsız Personel', appointments: 'Çoklu Şube' },
      features: ALL_FEATURES,
      cta: 'Premium\'a Geç',
    },
  ];

  allFeatureNames = ALL_FEATURES.map(f => f.name);

  faqItems: FAQ[] = [
    {
      question: 'Hangi tür işletmeler kullanabilir?',
      answer: 'Randevu alan her tür hizmet işletmesi için uygundur: berber, kuaför, güzellik salonu, masaj & spa, diş kliniği, klinik, dövme stüdyosu, fotoğrafçı, danışmanlık ofisi ve daha fazlası. Hizmetlerinizi ve çalışma saatlerinizi sisteme girmeniz yeterli.',
    },
    {
      question: 'Kayıt olmak için ne gerekiyor?',
      answer: 'Sadece bir e-posta adresi ve telefon numarası yeterli. 3 dakikada sisteme kayıt olup randevu almaya başlayabilirsiniz.',
    },
    {
      question: 'WhatsApp OTP doğrulama nasıl çalışıyor?',
      answer: 'Müşteri randevu alırken telefon numarasını girer, sisteme "Kodu Gönder" ile 6 haneli bir doğrulama kodu WhatsApp üzerinden iletilir. Kodu doğrulayan müşteri randevuyu tamamlayabilir. Bu sayede sahte veya hatalı numaralarla randevu alınması engellenir. Türkiye başta olmak üzere 13 ülke alan kodu desteklenir.',
    },
    {
      question: 'WhatsApp bildirimleri nasıl çalışıyor?',
      answer: 'Randevu oluşturulduğunda onay, 24 saat ve 1 saat öncesi hatırlatma, randevu tamamlandığında değerlendirme linki ve iptal durumunda bilgi mesajı müşterinize otomatik WhatsApp\'tan gönderilir. İşletme sahibine de yeni randevu geldiğinde anlık bildirim iletilir. Ek kurulum gerekmez.',
    },
    {
      question: 'Toplu WhatsApp mesajı özelliği ne işe yarar?',
      answer: 'Profesyonel ve Premium planlarda, kayıtlı tüm müşterilerinize tek tıkla özel bir WhatsApp mesajı gönderebilirsiniz. Kampanya duyurusu, indirim bildirimi veya tatil tebriki gibi iletişimlerde kullanabilirsiniz. Mesaj kaç kişiye ulaştı, kaç gönderim başarısız oldu diye sonuç özeti gösterilir.',
    },
    {
      question: 'Müşteri birden fazla hizmet alabilir mi?',
      answer: 'Evet. Müşteriler randevu alırken birden fazla hizmet seçebilir. Sistem toplam süreyi ve toplam ücreti otomatik hesaplar, uygun zaman dilimlerini buna göre gösterir.',
    },
    {
      question: 'Personel ve randevu limitleri nedir?',
      answer: 'Başlangıç planında 1 personel ve ayda 150 randevu desteklenir. Profesyonel ve Premium planlarda personel ve randevu sınırı yoktur. Başlangıç planında aylık limite yaklaşıldığında otomatik uyarı alırsınız.',
    },
    {
      question: 'Randevuları manuel onaylamak istiyorum, mümkün mü?',
      answer: 'Manuel randevu onayı tüm planlarda (Başlangıç dahil) mevcuttur. Manuel modda müşteri randevu talebi oluşturur; siz onayladığınızda müşteriye WhatsApp bildirim gönderilir. Bu mod özellikle yoğun dönemlerde veya kapasite kontrolü gerektiren işletmeler için idealdir. Otomatik modda ise randevu anında onaylanır.',
    },
    {
      question: 'Birden fazla şubem var, sistemi kullanabilir miyim?',
      answer: 'Premium planda çoklu şube yönetimi dahildir. Her şubenin kendi personeli, çalışma saatleri ve randevuları ayrı ayrı yönetilir; tüm şubelerinizi tek panel üzerinden takip edebilirsiniz. Başlangıç ve Profesyonel planlar tek şube içindir.',
    },
    {
      question: 'Personelime ayrı giriş hesabı tanımlayabilir miyim?',
      answer: 'Profesyonel ve Premium planlarda her personel için ayrı e-posta/şifre hesabı oluşturabilirsiniz. Personel yalnızca kendi randevularını, çalışma saatlerini ve izin günlerini görür. Müşteri iletişim bilgileri ve finansal veriler admin tarafında korunur.',
    },
    {
      question: 'Gelir ve gider takibi nasıl çalışıyor?',
      answer: 'Profesyonel ve Premium planlarda gelir & gider yönetimi dahildir. Giderlerinizi kategori bazlı (kira, elektrik, malzeme vb.) ekleyebilir, seçtiğiniz tarih aralığında toplam gelir, gider ve net kâr/zarar özetini anında görebilirsiniz.',
    },
    {
      question: 'İstediğim zaman plan değiştirebilir miyim?',
      answer: 'Evet! İstediğiniz zaman planınızı yükseltebilirsiniz. Değişiklik anında geçerli olur.',
    },
    {
      question: 'Verilerim güvende mi?',
      answer: 'Tüm verileriniz şifreli olarak Türkiye\'deki sunucularımızda saklanır ve düzenli olarak yedeklenir.',
    },
  ];

  // ── Lifecycle ─────────────────────────────────────────────────────────────
  get isLoggedIn(): boolean { return !!localStorage.getItem('accessToken'); }

  constructor(private router: Router, private sanitizer: DomSanitizer) {
    // Sanitize SVG icons so Angular's DomSanitizer doesn't strip them
    this.appFeatures = this.rawFeatures.map(f => ({
      icon: this.sanitizer.bypassSecurityTrustHtml(f.icon),
      title: f.title,
      desc: f.desc,
    }));
  }

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

  planHas(plan: Plan, featureName: string): boolean {
    return plan.features.find(f => f.name === featureName)?.included ?? false;
  }

  openMail(): void {
    window.open('mailto:ayarliyoinfo@gmail.com?subject=Bilgi%20Talebi', '_blank');
  }

  openWhatsApp(): void {
    window.open('https://wa.me/905000000000', '_blank');
  }
}
