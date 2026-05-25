import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { SuperAdminService, SuperAdminReports, RevenueReport } from '../../../core/services/superadmin.service';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-super-admin-reports',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './super-admin-reports.component.html',
  styleUrl: './super-admin-reports.component.scss'
})
export class SuperAdminReportsComponent implements OnInit {
  reports: SuperAdminReports | null = null;
  revenue: RevenueReport | null = null;
  isLoading = true;
  revenueLoading = true;
  errorMessage = '';
  showBroadcastModal = false;
  broadcastMessage = '';
  isBroadcasting = false;
  broadcastResult = '';

  constructor(private superAdminService: SuperAdminService) {}

  ngOnInit(): void {
    this.loadReports();
    this.loadRevenue();
  }

  loadReports(): void {
    this.isLoading = true;
    this.superAdminService.getReports().subscribe({
      next: (res) => { if (res.success) this.reports = res.data; this.isLoading = false; },
      error: () => { this.errorMessage = 'Raporlar yüklenemedi.'; this.isLoading = false; }
    });
  }

  loadRevenue(): void {
    this.revenueLoading = true;
    this.superAdminService.getRevenue().subscribe({
      next: (res) => { if (res.success) this.revenue = res.data; this.revenueLoading = false; },
      error: () => { this.revenueLoading = false; }
    });
  }

  sendBroadcast(): void {
    if (!this.broadcastMessage.trim()) return;
    this.isBroadcasting = true;
    this.superAdminService.broadcast(this.broadcastMessage).subscribe({
      next: (res) => {
        this.broadcastResult = res.message || 'Gönderildi.';
        this.broadcastMessage = '';
        this.isBroadcasting = false;
        setTimeout(() => { this.showBroadcastModal = false; this.broadcastResult = ''; }, 2500);
      },
      error: () => { this.isBroadcasting = false; }
    });
  }

  getMonthName(month: number): string {
    return ['Oca','Şub','Mar','Nis','May','Haz','Tem','Ağu','Eyl','Eki','Kas','Ara'][month - 1] ?? month.toString();
  }

  get maxGrowthCount(): number {
    if (!this.reports?.monthlyGrowth?.length) return 1;
    return Math.max(...this.reports.monthlyGrowth.map(m => m.count));
  }

  get maxRevenueMonth(): number {
    if (!this.revenue?.monthlyRevenue?.length) return 1;
    return Math.max(...this.revenue.monthlyRevenue.map(m => m.total), 1);
  }

  formatCurrency(val: number): string {
    return new Intl.NumberFormat('tr-TR', { style: 'currency', currency: 'TRY', maximumFractionDigits: 0 }).format(val);
  }
}
