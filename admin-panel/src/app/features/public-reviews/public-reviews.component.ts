import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { LogoComponent } from '../../shared/components/logo/logo.component';

interface ReviewItem {
  rating: number;
  customerName: string;
  comment: string | null;
  createdAt: string;
}

interface RatingData {
  totalReviews: number;
  averageRating: number;
  distribution: { star: number; count: number }[];
  reviews: ReviewItem[];
}

interface SalonData {
  name: string;
  logoUrl: string | null;
  themeColor: string | null;
}

@Component({
  selector: 'app-public-reviews',
  standalone: true,
  imports: [CommonModule, RouterLink, LogoComponent],
  templateUrl: './public-reviews.component.html',
  styleUrl: './public-reviews.component.scss',
})
export class PublicReviewsComponent implements OnInit {
  subdomain = '';
  salon: SalonData | null = null;
  data: RatingData | null = null;
  isLoading = true;
  error = '';

  constructor(private route: ActivatedRoute, private http: HttpClient) {}

  ngOnInit(): void {
    this.subdomain = this.route.snapshot.paramMap.get('subdomain') ?? '';
    this.loadData();
  }

  private loadData(): void {
    this.http.get<any>(`${environment.apiUrl}/booking/${this.subdomain}`).subscribe({
      next: (res) => {
        if (res.success) this.salon = res.data;
      },
    });

    this.http.get<any>(`${environment.apiUrl}/booking/${this.subdomain}/rating`).subscribe({
      next: (res) => {
        if (res.success) this.data = res.data;
        this.isLoading = false;
      },
      error: () => {
        this.error = 'Değerlendirmeler yüklenemedi.';
        this.isLoading = false;
      },
    });
  }

  getStars(rating: number): number[] {
    return Array.from({ length: 5 }, (_, i) => i + 1);
  }

  getBarWidth(star: number): number {
    if (!this.data || this.data.totalReviews === 0) return 0;
    const item = this.data.distribution.find(d => d.star === star);
    return item ? Math.round((item.count / this.data.totalReviews) * 100) : 0;
  }

  getCount(star: number): number {
    return this.data?.distribution.find(d => d.star === star)?.count ?? 0;
  }

  getInitials(name: string): string {
    return name.split(' ').map(n => n[0]).join('').toUpperCase().slice(0, 2);
  }

  formatDate(dateStr: string): string {
    return new Intl.DateTimeFormat('tr-TR', { day: 'numeric', month: 'long', year: 'numeric' }).format(new Date(dateStr));
  }
}
