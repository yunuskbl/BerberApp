import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { trigger, transition, style, animate } from '@angular/animations';

interface Feature { name: string; included: boolean; }
interface PlanLimit { staff: string; appointments: string; }
interface Plan {
  name: string; label: string; price: number; description: string;
  icon: string; featured: boolean; features: Feature[]; cta: string; ctaDisabled?: boolean;
  limits: PlanLimit;
}
interface FAQ { question: string; answer: string; open?: boolean; }

const ALL_FEATURES: Feature[] = [
  { name: 'Online Randevu Sayfası',        included: true },
  { name: 'Randevu Yönetimi',              included: true },
  { name: 'Müşteri & Personel Yönetimi',   included: true },
  { name: 'Hizmet & Fiyat Yönetimi',       included: true },
  { name: 'Tema & Logo Özelleştirme',      included: true },
  { name: 'Çoklu Dil (TR / EN / RU)',      included: true },
  { name: 'WhatsApp Bildirimleri',          included: true },
  { name: 'Randevu Hatırlatması',           included: true },
  { name: 'Fotoğraf Galerisi',             included: true },
  { name: 'Müşteri Değerlendirme Sistemi', included: true },
  { name: 'Rapor & Gelir Analizi',         included: true },
  { name: 'Öncelikli Destek',             included: true },
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
export class PricingComponent {

  appFeatures = [
    {
      icon: `<svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.75" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" d="M6.75 3v2.25M17.25 3v2.25M3 18.75V7.5a2.25 2.25 0 0 1 2.25-2.25h13.5A2.25 2.25 0 0 1 21 7.5v11.25m-18 0A2.25 2.25 0 0 0 5.25 21h13.5A2.25 2.25 0 0 0 21 18.75m-18 0v-7.5A2.25 2.25 0 0 1 5.25 9h13.5A2.25 2.25 0 0 1 21 11.25v7.5"/></svg>`,
      title: 'Online Randevu',
      desc: 'Müşterileriniz 7/24 internet üzerinden randevu alabilir. Telefon trafiğini sıfıra indirin.'
    },
    {
      icon: `<svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.75" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" d="M7.5 8.25h9m-9 3H12m-9.75 1.51c0 1.6 1.123 2.994 2.707 3.227 1.129.166 2.27.293 3.423.379.35.026.67.21.865.501L12 21l2.755-4.133a1.14 1.14 0 0 1 .865-.501 48.172 48.172 0 0 0 3.423-.379c1.584-.233 2.707-1.626 2.707-3.228V6.741c0-1.602-1.123-2.995-2.707-3.228A48.394 48.394 0 0 0 12 3c-2.392 0-4.744.175-7.043.513C3.373 3.746 2.25 5.14 2.25 6.741v6.018Z"/></svg>`,
      title: 'WhatsApp Bildirimleri',
      desc: 'Randevu onayı ve hatırlatma mesajları müşterilerinize otomatik olarak WhatsApp\'tan iletilir.'
    },
    {
      icon: `<svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.75" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" d="M11.48 3.499a.562.562 0 0 1 1.04 0l2.125 5.111a.563.563 0 0 0 .475.345l5.518.442c.499.04.701.663.321.988l-4.204 3.602a.563.563 0 0 0-.182.557l1.285 5.385a.562.562 0 0 1-.84.61l-4.725-2.885a.562.562 0 0 0-.586 0L6.982 20.54a.562.562 0 0 1-.84-.61l1.285-5.386a.562.562 0 0 0-.182-.557l-4.204-3.602a.562.562 0 0 1 .321-.988l5.518-.442a.563.563 0 0 0 .475-.345L11.48 3.5Z"/></svg>`,
      title: 'Değerlendirme Sistemi',
      desc: 'Hizmet sonrası müşteriden otomatik puan toplayın. Ortalama puanınız profil sayfanızda yayınlanır.'
    },
    {
      icon: `<svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.75" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" d="M3 13.125C3 12.504 3.504 12 4.125 12h2.25c.621 0 1.125.504 1.125 1.125v6.75C7.5 20.496 6.996 21 6.375 21h-2.25A1.125 1.125 0 0 1 3 19.875v-6.75ZM9.75 8.625c0-.621.504-1.125 1.125-1.125h2.25c.621 0 1.125.504 1.125 1.125v11.25c0 .621-.504 1.125-1.125 1.125h-2.25a1.125 1.125 0 0 1-1.125-1.125V8.625ZM16.5 4.125c0-.621.504-1.125 1.125-1.125h2.25C20.496 3 21 3.504 21 4.125v15.75c0 .621-.504 1.125-1.125 1.125h-2.25a1.125 1.125 0 0 1-1.125-1.125V4.125Z"/></svg>`,
      title: 'Gelir & Rapor Analizi',
      desc: 'Aylık gelir, doluluk oranı ve personel bazlı performans raporlarına anında erişin.'
    },
    {
      icon: `<svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.75" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" d="M15 19.128a9.38 9.38 0 0 0 2.625.372 9.337 9.337 0 0 0 4.121-.952 4.125 4.125 0 0 0-7.533-2.493M15 19.128v-.003c0-1.113-.285-2.16-.786-3.07M15 19.128v.106A12.318 12.318 0 0 1 8.624 21c-2.331 0-4.512-.645-6.374-1.766l-.001-.109a6.375 6.375 0 0 1 11.964-3.07M12 6.375a3.375 3.375 0 1 1-6.75 0 3.375 3.375 0 0 1 6.75 0Zm8.25 2.25a2.625 2.625 0 1 1-5.25 0 2.625 2.625 0 0 1 5.25 0Z"/></svg>`,
      title: 'Personel Yönetimi',
      desc: 'Personelinizi, çalışma saatlerini ve hizmetlerini kolayca yönetin. Randevuları personele atayın.'
    },
    {
      icon: `<svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.75" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" d="M9.53 16.122a3 3 0 0 0-5.78 1.128 2.25 2.25 0 0 1-2.4 2.245 4.5 4.5 0 0 0 8.4-2.245c0-.399-.078-.78-.22-1.128Zm0 0a15.998 15.998 0 0 0 3.388-1.62m-5.043-.025a15.994 15.994 0 0 1 1.622-3.395m3.42 3.42a15.995 15.995 0 0 0 4.764-4.648l3.876-5.814a1.151 1.151 0 0 0-1.597-1.597L14.146 6.32a15.996 15.996 0 0 0-4.649 4.763m3.42 3.42a6.776 6.776 0 0 0-3.42-3.42"/></svg>`,
      title: 'Marka Özelleştirme',
      desc: 'Logo, tema rengi ve fotoğraf galerinizle markanızı yansıtan profesyonel bir sayfa oluşturun.'
    },
  ];

  stats = [
    { value: '500+',  label: 'Aktif İşletme'    },
    { value: '50K+',  label: 'Aylık Randevu'     },
    { value: '%98',   label: 'Müşteri Memnuniyeti' },
    { value: '3 dk',  label: 'Kurulum Süresi'    },
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
        { name: 'Müşteri & Personel Yönetimi',   included: true  },
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
        { name: 'Müşteri & Personel Yönetimi',   included: true  },
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
      question: 'Değerlendirme sistemi nedir?',
      answer: 'Hizmet tamamlandığında müşteriye otomatik olarak bir değerlendirme linki gönderilir. Gelen puanlar ve yorumlar admin panelinizde görünür.',
    },
    {
      question: 'Personel ve randevu limitleri nedir?',
      answer: 'Başlangıç planında 1 personel ve ayda 100 randevu, Profesyonel planında 5 personel ve ayda 500 randevu desteklenmektedir. Premium planda personel ve randevu sayısı sınırsızdır. Müşteri sayısı tüm planlarda sınırsızdır.',
    },
    {
      question: 'Verilerim güvende mi?',
      answer: 'Tüm verileriniz şifreli olarak Türkiye\'deki sunucularımızda saklanır ve düzenli olarak yedeklenir.',
    },
  ];

  get isLoggedIn(): boolean { return !!localStorage.getItem('accessToken'); }

  constructor(private router: Router) {}

  goToApp(): void {
    this.router.navigate([this.isLoggedIn ? '/dashboard' : '/login']);
  }

  scrollTo(id: string): void {
    document.getElementById(id)?.scrollIntoView({ behavior: 'smooth' });
  }

  scrollToTop(): void {
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  selectPlan(_name: string): void {
    this.router.navigate([this.isLoggedIn ? '/settings' : '/login']);
  }

  toggleFAQ(i: number): void { this.faqItems[i].open = !this.faqItems[i].open; }

  planHas(plan: Plan, featureName: string): boolean {
    return plan.features.find(f => f.name === featureName)?.included ?? false;
  }
}
