import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { trigger, state, style, transition, animate } from '@angular/animations';

interface Plan {
  name: string;
  price: number;
  description: string;
  icon: string;
  featured: boolean;
  savings?: number;
  features: { name: string; included: boolean; icon: string }[];
  cta: string;
}

interface Testimonial {
  name: string;
  company: string;
  avatar: string;
  text: string;
  rating: number;
}

interface FAQ {
  question: string;
  answer: string;
  open?: boolean;
}

@Component({
  selector: 'app-pricing',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './pricing.component.html',
  styleUrls: ['./pricing.component.scss'],
  animations: [
    trigger('cardHover', [
      state('normal', style({ transform: 'translateY(0)', boxShadow: '0 4px 6px rgba(0,0,0,0.1)' })),
      state('hover', style({ transform: 'translateY(-8px)', boxShadow: '0 20px 40px rgba(99,102,241,0.2)' })),
      transition('normal <=> hover', animate('300ms ease-in-out'))
    ]),
    trigger('fadeIn', [
      transition(':enter', [
        style({ opacity: 0, transform: 'translateY(20px)' }),
        animate('600ms ease-out', style({ opacity: 1, transform: 'translateY(0)' }))
      ])
    ])
  ]
})
export class PricingComponent {
  plans: Plan[] = [
    {
      name: 'Basic',
      price: 899,
      description: 'Başlangıç için mükemmel',
      icon: '📱',
      featured: false,
      features: [
        { name: 'Randevu Yönetimi', included: true, icon: '📅' },
        { name: 'Müşteri Yönetimi', included: true, icon: '👥' },
        { name: 'Personel Yönetimi', included: true, icon: '👔' },
        { name: 'Hizmet Yönetimi', included: true, icon: '✂️' },
        { name: 'WhatsApp Bildirimleri', included: false, icon: '💬' },
        { name: 'Değerlendirme Sistemi', included: false, icon: '⭐' },
        { name: 'Google Maps Entegrasyonu', included: false, icon: '🗺️' },
        { name: 'Rapor & Analytics', included: false, icon: '📊' },
        { name: 'Tekrarlayan Müşteri Analizi', included: false, icon: '🔄' },
        { name: 'Gelişmiş Raporlar', included: false, icon: '📈' },
        { name: '24/7 Destek', included: false, icon: '🆘' }
      ],
      cta: 'Mevcut Plan'
    },
    {
      name: 'Standard',
      price: 1799,
      description: 'En popüler seçim',
      icon: '⭐',
      featured: true,
      savings: 17,
      features: [
        { name: 'Randevu Yönetimi', included: true, icon: '📅' },
        { name: 'Müşteri Yönetimi', included: true, icon: '👥' },
        { name: 'Personel Yönetimi', included: true, icon: '👔' },
        { name: 'Hizmet Yönetimi', included: true, icon: '✂️' },
        { name: 'WhatsApp Bildirimleri', included: true, icon: '💬' },
        { name: 'Değerlendirme Sistemi', included: true, icon: '⭐' },
        { name: 'Google Maps Entegrasyonu', included: true, icon: '🗺️' },
        { name: 'Rapor & Analytics', included: true, icon: '📊' },
        { name: 'Tekrarlayan Müşteri Analizi', included: false, icon: '🔄' },
        { name: 'Gelişmiş Raporlar', included: false, icon: '📈' },
        { name: '24/7 Destek', included: false, icon: '🆘' }
      ],
      cta: 'Standard\'e Yükselt'
    },
    {
      name: 'Full',
      price: 2399,
      description: 'Profesyoneller için',
      icon: '👑',
      featured: false,
      savings: 0,
      features: [
        { name: 'Randevu Yönetimi', included: true, icon: '📅' },
        { name: 'Müşteri Yönetimi', included: true, icon: '👥' },
        { name: 'Personel Yönetimi', included: true, icon: '👔' },
        { name: 'Hizmet Yönetimi', included: true, icon: '✂️' },
        { name: 'WhatsApp Bildirimleri', included: true, icon: '💬' },
        { name: 'Değerlendirme Sistemi', included: true, icon: '⭐' },
        { name: 'Google Maps Entegrasyonu', included: true, icon: '🗺️' },
        { name: 'Rapor & Analytics', included: true, icon: '📊' },
        { name: 'Tekrarlayan Müşteri Analizi', included: true, icon: '🔄' },
        { name: 'Gelişmiş Raporlar', included: true, icon: '📈' },
        { name: '24/7 Destek', included: true, icon: '🆘' }
      ],
      cta: 'Full\'a Yükselt'
    }
  ];

  testimonials: Testimonial[] = [
    {
      name: 'Ayşe Yılmaz',
      company: 'Beauty Salon Istanbul',
      avatar: '👩‍🦰',
      text: 'WhatsApp bildirimleri sayesinde müşterilerim randevularını asla unutmuyor. Memnuniyet %40 arttı!',
      rating: 5
    },
    {
      name: 'Mehmet Kaya',
      company: 'Elite Barber Shop',
      avatar: '👨‍💼',
      text: 'Değerlendirme sistemi ve tekrarlayan müşteri analizi sayesinde hangi müşterilerimi daha iyi tanıyorum. Harika özellik!',
      rating: 5
    },
    {
      name: 'Zeynep Demir',
      company: 'Hair Design Studio',
      avatar: '👩‍🎨',
      text: 'Google Maps entegrasyonu müşterilerimin salonu bulmasını çok kolaylaştırdı. Full paketi kesinlikle değer!',
      rating: 5
    }
  ];

  faqItems: FAQ[] = [
    {
      question: 'İstediğim zaman plan değiştirebilir miyim?',
      answer: 'Evet! Herhangi bir zamanda plan değiştirebilir veya iptal edebilirsiniz. Ödeminiz günlere göre hesaplanacaktır.',
      open: false
    },
    {
      question: 'WhatsApp bildirimleri nasıl çalışıyor?',
      answer: 'Randevu tamamlandığında müşterinize otomatik olarak WhatsApp mesajı gönderilir. Mesajda değerlendirme linki ve salonunuza Google Maps yönlendirmesi yer alır. Standard ve Full paketlerinde mevcuttur.',
      open: false
    },
    {
      question: 'Değerlendirme sistemi nasıl çalışıyor?',
      answer: 'Randevu tamamlandıktan sonra müşterileriniz 1-5 yıldız arası puan ve yorum bırakabilir. Yönetici panelinden tüm değerlendirmeleri görebilir, filtreleyebilirsiniz. Standard ve Full paketlerinde mevcuttur.',
      open: false
    },
    {
      question: 'Tekrarlayan müşteri analizi ne işe yarar?',
      answer: 'Full pakette sunulan bu özellik sayesinde hangi müşterilerin daha önce de yorum bıraktığını görebilirsiniz. Sadık müşterilerinizi tanıyarak onlara özel kampanyalar oluşturabilirsiniz.',
      open: false
    },
    {
      question: 'Google Maps entegrasyonu nasıl çalışıyor?',
      answer: 'Standard ve Full paketlerinde, gönderilen WhatsApp mesajlarına otomatik olarak salonunuzun Google Maps yönlendirme linki eklenir. Müşterileriniz tek tıkla yol tarifi alabilir.',
      open: false
    },
    {
      question: 'Kişi sayısının bir sınırı var mı?',
      answer: 'Hayır! Tüm planlarda sınırsız müşteri ve personel ekleyebilirsiniz.',
      open: false
    }
  ];

  cardHoverStates: { [key: string]: string } = {
    Basic: 'normal',
    Standard: 'normal',
    Full: 'normal'
  };

  constructor(private router: Router) {}

  selectPlan(plan: string) {
    if (plan === 'Basic') {
      alert('Zaten Basic plan\'dadasınız!');
      return;
    }
    alert(`${plan} paketine yükseltme işlemi başladı!`);
    this.router.navigate(['/dashboard']);
  }

  toggleFAQ(index: number) {
    this.faqItems[index].open = !this.faqItems[index].open;
  }

  onCardHover(planName: string, state: string) {
    this.cardHoverStates[planName] = state;
  }

  renderStars(rating: number): string {
    return '⭐'.repeat(rating);
  }
}