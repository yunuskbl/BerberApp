import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { PLANS, formatPrice } from '../../core/pricing';

interface PlanInfo {
  name: string;
  label: string;
  price: number;
  icon: string;
  color: string;
  extras: string[];
}

@Component({
  selector: 'app-upgrade',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './upgrade.component.html',
  styleUrl: './upgrade.component.scss',
})
export class UpgradeComponent implements OnInit {
  currentPlanName  = 'Baslangic';
  requiredPlanName = 'Profesyonel';

  private readonly plans: Record<string, PlanInfo> = {
    Baslangic: {
      name: 'Baslangic', label: 'Başlangıç', price: PLANS['baslangic'].monthly, icon: '🌱', color: '#6b7280',
      extras: [],
    },
    Profesyonel: {
      name: 'Profesyonel', label: 'Profesyonel', price: PLANS['profesyonel'].monthly, icon: '⚡', color: '#7c3aed',
      extras: [
        'Toplu WhatsApp Kampanyası',
        'Gelir & Gider Yönetimi',
        'Personel Girişi & Yetkilendirme',
        '5 Personele Kadar',
        'Aylık 500 Randevu',
      ],
    },
    Premium: {
      name: 'Premium', label: 'Premium', price: PLANS['premium'].monthly, icon: '👑', color: '#d97706',
      extras: [
        'Çoklu Şube Yönetimi',
        'Sınırsız Personel',
        'Öncelikli Destek',
        'Merkezi Raporlama',
        'Tüm Profesyonel Özellikler',
      ],
    },
  };

  get current(): PlanInfo  { return this.plans[this.currentPlanName]  ?? this.plans['Baslangic']; }
  get required(): PlanInfo { return this.plans[this.requiredPlanName] ?? this.plans['Profesyonel']; }

  constructor(private route: ActivatedRoute, private router: Router) {}

  ngOnInit(): void {
    this.route.queryParams.subscribe(p => {
      this.currentPlanName  = p['current']  || 'Baslangic';
      this.requiredPlanName = p['required'] || 'Profesyonel';
    });
  }

  /**
   * Yükseltme konuşmayla ilerliyor. Satış yüz yüze yapıldığı için buradan
   * havale formuna göndermek, işletmeyi kişisel bir IBAN'a para yollayıp
   * onay beklemeye bırakıyordu; bir mesaj ya da telefon hem daha hızlı hem
   * de satışın gerçekten yapıldığı kanal.
   *
   * Havale formu (/payment) silinmedi: doğrudan bağlantıyla hâlâ açılıyor,
   * böylece "havale ile ödeyeyim" diyen işletmeye link gönderilebiliyor.
   */
  contactUrl(): string {
    const plan = this.required.label;
    const text = encodeURIComponent(
      `Merhaba, ayarlıyo'da ${plan} planına geçmek istiyorum.`);
    return `https://wa.me/905305606916?text=${text}`;
  }

  readonly supportPhone = '0530 560 69 16';

  fmt(value: number): string { return formatPrice(value); }

  goBack(): void { this.router.navigate(['/dashboard']); }
}
