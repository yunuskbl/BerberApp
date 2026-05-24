import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { HttpClient, HttpParams } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { Title } from '@angular/platform-browser';
import { environment } from '../../../environments/environment';
import { LanguageSwitcherComponent } from '../../shared/components/language-switcher/language-switcher.component';
import { TranslatePipe } from '../../shared/pipes/translate.pipe';
import { DecorativeBgComponent } from '../../shared/components/decorative-bg/decorative-bg.component';
import { LogoComponent } from '../../shared/components/logo/logo.component';

interface SalonPhoto { id: string; url: string; }

interface Salon {
  id:             string;
  name:           string;
  subdomain:      string;
  phone?:         string;
  address?:       string;
  city?:          string;
  logoUrl?:       string;
  themeColor?:    string;
  businessType?:  string;
  averageRating?: number;
  totalReviews?:  number;
  photos?:        SalonPhoto[];
  distance?:      number; // km
}

export type SortOption = 'rating' | 'reviews' | 'newest' | 'name' | 'distance';
export type LocationMode = 'nearby' | 'city' | 'all' | 'notFound' | null;

interface BusinessTypeOption { value: string; label: string; emoji: string; }


@Component({
  selector: 'app-salons',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule,
            LanguageSwitcherComponent, TranslatePipe,
            DecorativeBgComponent, LogoComponent],
  templateUrl: './salons.component.html',
  styleUrl: './salons.component.scss'
})
export class SalonsComponent implements OnInit {
  salons:       Salon[]       = [];
  isLoading     = true;
  searchQuery   = '';
  errorMessage  = '';
  locationMode: LocationMode  = null;

  // Konum
  userLat: number | null = null;
  userLon: number | null = null;
  locationStatus: 'idle' | 'requesting' | 'granted' | 'denied' | 'unsupported' = 'idle';
  locationFilterActive = false;

  // İşletme türü filtresi
  selectedBusinessType: string | null = null;
  readonly businessTypeOptions: BusinessTypeOption[] = [
    { value: 'Berber',          label: 'Berber',           emoji: '✂️' },
    { value: 'Kuafor',          label: 'Kuaför',           emoji: '💇' },
    { value: 'GüzellikSalonu',  label: 'Güzellik Salonu',  emoji: '💅' },
    { value: 'MasajSpa',        label: 'Masaj & Spa',      emoji: '🧖' },
    { value: 'DisKlinigi',      label: 'Diş Kliniği',      emoji: '🦷' },
    { value: 'Klinik',          label: 'Klinik',           emoji: '🏥' },
    { value: 'Diger',           label: 'Diğer',            emoji: '🏪' },
  ];

  // Sıralama
  sortBy: SortOption = 'rating';
  readonly sortOptions: { value: SortOption; label: string; icon: string }[] = [
    { value: 'rating',   label: 'En Yüksek Puan',        icon: '⭐' },
    { value: 'reviews',  label: 'En Fazla Değerlendirme', icon: '💬' },
    { value: 'distance', label: 'Yakınımdakiler',         icon: '📍' },
    { value: 'newest',   label: 'Yeni Eklenenler',        icon: '🆕' },
    { value: 'name',     label: 'A-Z',                    icon: '🔤' },
  ];

  // Carousel
  carouselIndex = new Map<string, number>();
  touchStartX   = new Map<string, number>();

  getIdx(id: string): number { return this.carouselIndex.get(id) ?? 0; }

  prevPhoto(salonId: string, photos: SalonPhoto[], e: Event): void {
    e.preventDefault(); e.stopPropagation();
    const cur = this.getIdx(salonId);
    this.carouselIndex.set(salonId, (cur - 1 + photos.length) % photos.length);
  }

  nextPhoto(salonId: string, photos: SalonPhoto[], e: Event): void {
    e.preventDefault(); e.stopPropagation();
    const cur = this.getIdx(salonId);
    this.carouselIndex.set(salonId, (cur + 1) % photos.length);
  }

  goToPhoto(salonId: string, idx: number, e: Event): void {
    e.preventDefault(); e.stopPropagation();
    this.carouselIndex.set(salonId, idx);
  }

  onTouchStart(salonId: string, e: TouchEvent): void {
    this.touchStartX.set(salonId, e.touches[0].clientX);
  }

  onTouchEnd(salonId: string, photos: SalonPhoto[], e: TouchEvent): void {
    const startX = this.touchStartX.get(salonId);
    if (startX === undefined) return;
    const deltaX = e.changedTouches[0].clientX - startX;
    this.touchStartX.delete(salonId);
    if (Math.abs(deltaX) < 30) return;
    deltaX < 0 ? this.nextPhoto(salonId, photos, e) : this.prevPhoto(salonId, photos, e);
  }

  constructor(private http: HttpClient, private titleService: Title) {}

  ngOnInit(): void {
    this.titleService.setTitle('Salonlar - ayarlıyo');
    this.loadSalons();
  }

  // ── Konum izni ────────────────────────────────────────────────────────────
  requestLocation(): void {
    if (!navigator.geolocation) {
      this.locationStatus = 'unsupported';
      return;
    }
    this.locationStatus = 'requesting';
    navigator.geolocation.getCurrentPosition(
      (pos) => {
        this.userLat            = pos.coords.latitude;
        this.userLon            = pos.coords.longitude;
        this.locationStatus     = 'granted';
        this.locationFilterActive = true;
        this.loadSalons();
      },
      () => {
        this.locationStatus = 'denied';
      },
      { timeout: 8000, maximumAge: 300_000 }
    );
  }

  onLocationToggle(): void {
    if (this.locationFilterActive) {
      this.locationFilterActive = false;
      if (this.sortBy === 'distance') this.sortBy = 'rating';
      this.loadSalons();
      return;
    }
    if (this.userLat !== null) {
      this.locationFilterActive = true;
      this.loadSalons();
    } else {
      this.requestLocation();
    }
  }

  // ── Salon yükle ───────────────────────────────────────────────────────────
  loadSalons(): void {
    this.isLoading = true;
    let params = new HttpParams().set('sortBy', this.sortBy);
    if (this.searchQuery) params = params.set('search', this.searchQuery);
    if (this.locationFilterActive && this.userLat !== null && this.userLon !== null) {
      params = params.set('userLat', this.userLat.toString());
      params = params.set('userLon', this.userLon.toString());
    }
    if (this.selectedBusinessType) params = params.set('businessType', this.selectedBusinessType);

    this.http.get<any>(`${environment.apiUrl}/salons`, { params }).subscribe({
      next: (res) => {
        if (res.success) {
          this.salons       = res.data;
          this.locationMode = res.locationMode ?? 'all';
        }
        this.isLoading = false;
      },
      error: () => {
        this.errorMessage = 'Salonlar yüklenemedi.';
        this.isLoading    = false;
      }
    });
  }

  onSearch(): void { this.loadSalons(); }

  resetSearch(): void {
    this.searchQuery = '';
    this.loadSalons();
  }

  onSortChange(val: SortOption): void {
    this.sortBy = val;
    if (val === 'distance' && !this.locationFilterActive) {
      // Mesafe seçildi ama konum filtresi kapalı → aç
      if (this.userLat !== null) {
        this.locationFilterActive = true;
      } else {
        this.requestLocation();
        return;
      }
    }
    this.loadSalons();
  }

  onBusinessTypeFilter(value: string | null): void {
    this.selectedBusinessType = this.selectedBusinessType === value ? null : value;
    this.loadSalons();
  }

  get locationBannerText(): string {
    switch (this.locationMode) {
      case 'nearby': return '📍 Yakınınizdaki salonlar';
      case 'city':   return '🏙 Şehrinizdeki salonlar';
      default:       return '';
    }
  }

  formatDistance(km: number): string {
    if (km < 1) return `${Math.round(km * 1000)} m`;
    return `${km.toFixed(1)} km`;
  }

  getInitials(name: string): string {
    return name.split(' ').map(n => n[0]).join('').toUpperCase().slice(0, 2);
  }

  getBusinessTypeEmoji(type: string): string {
    return this.businessTypeOptions.find(o => o.value === type)?.emoji ?? '🏪';
  }

  get activeSortLabel(): string {
    return this.sortOptions.find(o => o.value === this.sortBy)?.label ?? '';
  }
}
