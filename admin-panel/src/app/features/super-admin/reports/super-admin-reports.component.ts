import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { SuperAdminService, SuperAdminReports } from '../../../core/services/superadmin.service';

@Component({
  selector: 'app-super-admin-reports',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './super-admin-reports.component.html',
  styleUrl: './super-admin-reports.component.scss'
})
export class SuperAdminReportsComponent implements OnInit {
  reports: SuperAdminReports | null = null;
  isLoading = true;
  errorMessage = '';

  constructor(private superAdminService: SuperAdminService) {}

  ngOnInit(): void {
    this.loadReports();
  }

  loadReports(): void {
    this.isLoading = true;
    this.superAdminService.getReports().subscribe({
      next: (res) => {
        if (res.success) {
          this.reports = res.data;
        }
        this.isLoading = false;
      },
      error: () => {
        this.errorMessage = 'Raporlar yüklenemedi.';
        this.isLoading = false;
      }
    });
  }

  getMonthName(month: number): string {
    const names = ['Oca', 'Şub', 'Mar', 'Nis', 'May', 'Haz', 'Tem', 'Ağu', 'Eyl', 'Eki', 'Kas', 'Ara'];
    return names[month - 1] ?? month.toString();
  }

  get maxGrowthCount(): number {
    if (!this.reports?.monthlyGrowth?.length) return 1;
    return Math.max(...this.reports.monthlyGrowth.map(m => m.count));
  }
}
