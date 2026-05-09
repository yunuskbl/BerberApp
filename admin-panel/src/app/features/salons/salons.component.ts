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
}

@Component({
  selector: 'app-salons',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule, LanguageSwitcherComponent, TranslatePipe, DecorativeBgComponent, LogoComponent],
  templateUrl: './salons.component.html',
  styleUrl: './salons.component.scss'
})
export class SalonsComponent implements OnInit {
  salons:      Salon[] = [];
  isLoading    = true;
  searchQuery  = '';
  errorMessage = '';

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

  getInitials(name: string): string {
    return name.split(' ').map(n => n[0]).join('').toUpperCase().slice(0, 2);
  }
}