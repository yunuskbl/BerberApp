import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { trigger, state, style, transition, animate } from '@angular/animations';

interface Feature {
  name: string;
  included: boolean;
}

interface Plan {
  name: string;
  label: string;
  price: number;
  description: string;
  icon: string;
  featured: boolean;
  features: Feature[];
  cta: string;
  ctaDisabled?: boolean;
}

interface FAQ {
  question: string;
  answer: string;
  open?: boolean;
}

const ALL_FEATURES: Feature[] = [
  { name: 'Online Randevu Sayfası',          included: true },
  { name: 'Randevu Yönetimi',                included: true },
  { name: 'Müşteri & Personel Yönetimi',     included: true },
  { name: 'Hizmet & Fiyat Yönetimi',         included: true },
  { name: 'Tema & Logo Özelleştirme',        included: true },
  { name: 'Çoklu Dil (TR / EN / RU)',        included: true },
  { name: 'WhatsApp Bildirimleri',            included: true },
  { name: 'Randevu Hatırlatması',             included: true },
  { name: 'Fotoğraf Galerisi',               included: true },
  { name: 'Müşteri Değerlendirme Sistemi',   included: true },
  { name: 'Rapor & Gelir Analizi',           included: true },
  { name: 'Öncelikli Destek',               included: true },
];

@Component({
  selector: 'app-pricing',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './pricing.component.html',
  styleUrls: ['./pricing.component.scss'],
  animations: [
    trigger('fadeIn', [
      transition(':enter', [
        style({ opacity: 0, transform: 'translateY(16px)' }),
        animate('500ms ease-out', style({ opacity: 1, transform: 'translateY(0)' }))
      ])
    ])
  ]
})
export class PricingComponent {

  plans: Plan[] = [
    {
      name: 'baslangic',
      label: 'Başlangıç',
      price: 899,
      description: 'Küçük işletmeler için temel araçlar',
      icon: '🌱',
      featured: false,
      ctaDisabled: true,
      features: [
        { name: 'Online Randevu Sayfası',          included: true  },
        { name: 'Randevu Yönetimi',                included: true  },
        { name: 'Müşteri & Personel Yönetimi',     included: true  },
        { name: 'Hizmet & Fiyat Yönetimi',         included: true  },
        { name: 'Tema & Logo Özelleştirme',        included: true  },
        { name: 'Çoklu Dil (TR / EN / RU)',        included: true  },
        { name: 'WhatsApp Bildirimleri',            included: false },
        { name: 'Randevu Hatırlatması',             included: false },
        { name: 'Fotoğraf Galerisi',               included: false },
        { name: 'Müşteri Değerlendirme Sistemi',   included: false },
        { name: 'Rapor & Gelir Analizi',           included: false },
        { name: 'Öncelikli Destek',               included: false },
      ],
      cta: 'Mevcut Plan',
    },
    {
      name: 'profesyonel',
      label: 'Profesyonel',
      price: 1799,
      description: 'Büyüyen işletmeler için tam donanım',
      icon: '⚡',
      featured: true,
      features: [
        { name: 'Online Randevu Sayfası',          included: true  },
        { name: 'Randevu Yönetimi',                included: true  },
        { name: 'Müşteri & Personel Yönetimi',     included: true  },
        { name: 'Hizmet & Fiyat Yönetimi',         included: true  },
        { name: 'Tema & Logo Özelleştirme',        included: true  },
        { name: 'Çoklu Dil (TR / EN / RU)',        included: true  },
        { name: 'WhatsApp Bildirimleri',            included: true  },
        { name: 'Randevu Hatırlatması',             included: true  },
        { name: 'Fotoğraf Galerisi',               included: true  },
        { name: 'Müşteri Değerlendirme Sistemi',   included: true  },
        { name: 'Rapor & Gelir Analizi',           included: false },
        { name: 'Öncelikli Destek',               included: false },
      ],
      cta: 'Profesyonel\'e Geç',
    },
    {
      name: 'premium',
      label: 'Premium',
      price: 2399,
      description: 'Profesyonel salonlar için tüm özellikler',
      icon: '👑',
      featured: false,
      features: ALL_FEATURES,
      cta: 'Premium\'a Geç',
    },
  ];

  allFeatureNames = ALL_FEATURES.map(f => f.name);

  faqItems: FAQ[] = [
    {
      question: 'İstediğim zaman plan değiştirebilir miyim?',
      answer: 'Evet! İstediğiniz zaman planınızı yükseltebilir veya düşürebilirsiniz. Değişiklik anında geçerli olur.',
      open: false,
    },
    {
      question: 'WhatsApp bildirimleri nasıl çalışıyor?',
      answer: 'Randevu onayı, iptal ve hatırlatma mesajları müşterinizin telefonuna otomatik olarak WhatsApp üzerinden gönderilir. Ek bir kurulum gerekmez.',
      open: false,
    },
    {
      question: 'Değerlendirme sistemi nedir?',
      answer: 'Hizmet tamamlandığında müşteriye otomatik olarak bir değerlendirme linki gönderilir. Gelen puanlar ve yorumlar admin panelinizde görünür, ortalama puan salons sayfanızda yayınlanır.',
      open: false,
    },
    {
      question: 'Kişi veya randevu sayısı sınırı var mı?',
      answer: 'Hayır! Tüm planlarda sınırsız müşteri, personel ve randevu ekleyebilirsiniz.',
      open: false,
    },
    {
      question: 'Verilerim güvende mi?',
      answer: 'Tüm verileriniz şifreli olarak Türkiye\'deki sunucularımızda saklanır ve günlük olarak yedeklenir.',
      open: false,
    },
  ];

  constructor(private router: Router) {}

  selectPlan(name: string): void {
    if (name === 'baslangic') return;
    this.router.navigate(['/settings']);
  }

  toggleFAQ(i: number): void {
    this.faqItems[i].open = !this.faqItems[i].open;
  }

  planHas(plan: Plan, featureName: string): boolean {
    return plan.features.find(f => f.name === featureName)?.included ?? false;
  }
}
