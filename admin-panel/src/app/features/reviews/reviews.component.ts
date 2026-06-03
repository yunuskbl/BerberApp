import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { TranslatePipe } from '../../shared/pipes/translate.pipe';
import { LanguageService } from '../../core/services/language.service';

interface Review {
  id: string;
  rating: number;
  customerName: string;
  customerId: string | null;
  customerPhone: string | null;
  comment: string | null;
  createdAt: string;
  isRepeatCustomer: boolean;
}

interface ReviewSummary {
  total: number;
  average: number;
  distribution: { star: number; count: number }[];
  reviews: Review[];
}

@Component({
  selector: 'app-reviews',
  standalone: true,
  imports: [CommonModule, TranslatePipe],
  templateUrl: './reviews.component.html',
  styleUrl:    './reviews.component.scss',
})
export class ReviewsComponent implements OnInit {
  isLoading = true;
  data: ReviewSummary = { total: 0, average: 0, distribution: [], reviews: [] };

  /** Arama sorgusu (isim veya telefon) */
  searchQuery = '';

  /** Seçili müşteri filtresi (customerId veya customerName) */
  selectedCustomerId: string | null = null;
  selectedCustomerName: string | null = null;

  /** Tekrar eden müşteri yorumlarını gizle */
  hideRepeat = false;

  readonly stars = [5, 4, 3, 2, 1];

  constructor(
    private http: HttpClient,
    public lang: LanguageService,
  ) {}

  ngOnInit(): void {
    this.http.get<any>(`${environment.apiUrl}/reviews`).subscribe({
      next: r => { if (r.success) this.data = r.data; this.isLoading = false; },
      error: ()  => { this.isLoading = false; },
    });
  }

  /** Gösterilecek filtrelenmiş yorum listesi */
  get filteredReviews(): Review[] {
    let list = this.data.reviews;

    // Arama filtresi (isim veya telefon)
    const q = this.searchQuery.trim().toLowerCase();
    if (q) {
      list = list.filter(r =>
        r.customerName.toLowerCase().includes(q) ||
        (r.customerPhone ?? '').toLowerCase().replace(/\s/g, '').includes(q.replace(/\s/g, ''))
      );
    }

    // Müşteri filtresi
    if (this.selectedCustomerId) {
      list = list.filter(r => r.customerId === this.selectedCustomerId);
    }

    // Tekrar eden müşteri filtresi: her müşteriden yalnızca ilk (en yeni) yorumu göster
    if (this.hideRepeat && !this.selectedCustomerId) {
      const seen = new Set<string>();
      list = list.filter(r => {
        const key = r.customerId ?? r.customerName;
        if (seen.has(key)) return false;
        seen.add(key);
        return true;
      });
    }

    return list;
  }

  onSearch(event: Event): void {
    this.searchQuery = (event.target as HTMLInputElement).value;
    this.clearCustomerFilter();
  }

  clearSearch(): void {
    this.searchQuery = '';
  }

  /** Tekrar eden müşteri var mı? */
  get hasRepeatCustomers(): boolean {
    return this.data.reviews.some(r => r.isRepeatCustomer);
  }

  /** Müşteri adına tıklayınca filtrele */
  filterByCustomer(review: Review): void {
    if (this.selectedCustomerId === review.customerId && this.selectedCustomerName === review.customerName) {
      this.clearCustomerFilter();
      return;
    }
    this.selectedCustomerId   = review.customerId;
    this.selectedCustomerName = review.customerName;
    this.hideRepeat = false;
  }

  clearCustomerFilter(): void {
    this.selectedCustomerId   = null;
    this.selectedCustomerName = null;
  }

  clearFilters(): void {
    this.searchQuery          = '';
    this.selectedCustomerId   = null;
    this.selectedCustomerName = null;
    this.hideRepeat           = false;
  }

  starArray(n: number): number[] { return Array(n).fill(0); }
  emptyStarArray(n: number): number[] { return Array(5 - n).fill(0); }

  barWidth(star: number): string {
    if (!this.data.total) return '0%';
    const entry = this.data.distribution.find(d => d.star === star);
    return `${Math.round(((entry?.count ?? 0) / this.data.total) * 100)}%`;
  }

  barCount(star: number): number {
    return this.data.distribution.find(d => d.star === star)?.count ?? 0;
  }

  formatDate(dateStr: string): string {
    return new Date(dateStr).toLocaleDateString(this.lang.dateLocale, {
      day: 'numeric', month: 'long', year: 'numeric',
    });
  }

  get filledStars(): number { return Math.round(this.data.average); }
}
