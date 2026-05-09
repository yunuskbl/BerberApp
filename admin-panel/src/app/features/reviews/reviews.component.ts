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
  comment: string | null;
  createdAt: string;
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
