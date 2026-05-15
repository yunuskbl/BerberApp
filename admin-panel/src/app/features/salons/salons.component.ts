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
  logoUrl?:       string;
  themeColor?:    string;
  averageRating?: number;
  totalReviews?:  number;
  photos?:        SalonPhoto[];
}

@Component({
  selector: 'app-salons',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule, LanguageSwitcherComponent, TranslatePipe, DecorativeBgComponent, LogoComponent],
  templateUrl: './salons.component.html',
  styleUrl: './salons.component.scss'
})
export class SalonsComponent implements OnInit {
  salons:        Salon[] = [];
  isLoading      = true;
  searchQuery    = '';
  errorMessage   = '';
  carouselIndex  = new Map<string, number>();

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

  constructor(private http: HttpClient, private titleService: Title) {}

  ngOnInit(): void {
    this.titleService.setTitle('Salonlar - ayarlıyo');
    this.loadSalons();
  }

  loadSalons(): void {
    this.isLoading = true;
    let params = new HttpParams();
    if (this.searchQuery) params = params.set('search', this.searchQuery);

    this.http.get<any>(`${environment.apiUrl}/salons`, { params }).subscribe({
      next: (res) => {
        if (res.success) this.salons = res.data;
        this.isLoading = false;
      },
      error: () => {
        this.errorMessage = 'Salonlar yüklenemedi.';
        this.isLoading    = false;
      }
    });
  }

  onSearch(): void {
    this.loadSalons();
  }

  resetSearch(): void {
    this.searchQuery = '';
    this.loadSalons();
  }

  getInitials(name: string): string {
    return name.split(' ').map(n => n[0]).join('').toUpperCase().slice(0, 2);
  }
}