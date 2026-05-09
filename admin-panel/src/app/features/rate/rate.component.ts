import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { LogoComponent } from '../../shared/components/logo/logo.component';

@Component({
  selector: 'app-rate',
  standalone: true,
  imports: [CommonModule, FormsModule, LogoComponent],
  templateUrl: './rate.component.html',
  styleUrl: './rate.component.scss',
})
export class RateComponent implements OnInit {
  subdomain    = '';
  appointmentId = '';
  hoveredStar  = 0;
  selectedStar = 0;
  comment      = '';
  isSubmitting = false;
  isSubmitted  = false;
  alreadyDone  = false;
  errorMsg     = '';
  salonName    = '';

  constructor(
    private route: ActivatedRoute,
    private http: HttpClient,
  ) {}

  ngOnInit(): void {
    this.subdomain     = this.route.snapshot.paramMap.get('subdomain') ?? '';
    this.appointmentId = this.route.snapshot.paramMap.get('appointmentId') ?? '';
    this.loadSalonName();
  }

  loadSalonName(): void {
    this.http
      .get<any>(`${environment.apiUrl}/booking/${this.subdomain}`)
      .subscribe({ next: r => { if (r.success) this.salonName = r.data.name; } });
  }

  stars = [1, 2, 3, 4, 5];

  starLabel(n: number): string {
    return ['', 'Berbat', 'Kötü', 'İdare Eder', 'İyi', 'Harika!'][n];
  }

  onHover(n: number): void { this.hoveredStar = n; }
  onLeave(): void           { this.hoveredStar = 0; }
  onSelect(n: number): void { this.selectedStar = n; }

  get displayStar(): number { return this.hoveredStar || this.selectedStar; }

  submit(): void {
    if (this.selectedStar === 0 || this.isSubmitting) return;
    this.isSubmitting = true;
    this.errorMsg     = '';

    this.http
      .post<any>(
        `${environment.apiUrl}/booking/${this.subdomain}/reviews/${this.appointmentId}`,
        { rating: this.selectedStar, comment: this.comment.trim() || null },
      )
      .subscribe({
        next: () => {
          this.isSubmitted  = true;
          this.isSubmitting = false;
        },
        error: err => {
          this.isSubmitting = false;
          if (err.status === 409) {
            this.alreadyDone = true;
          } else {
            this.errorMsg = err.error?.message || 'Bir hata oluştu. Lütfen tekrar deneyin.';
          }
        },
      });
  }
}
