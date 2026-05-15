import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { SuperAdminService, SuperAdminSubscription } from '../../../core/services/superadmin.service';

@Component({
  selector: 'app-super-admin-payments',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './super-admin-payments.component.html',
  styleUrl: './super-admin-payments.component.scss'
})
export class SuperAdminPaymentsComponent implements OnInit {
  subscriptions: SuperAdminSubscription[] = [];
  isLoading = true;
  errorMessage = '';
  selectedStatus = '';
  searchQuery = '';

  readonly statusOptions = [
    { value: '', label: 'Tümü' },
    { value: 'Active', label: 'Aktif' },
    { value: 'Trial', label: 'Deneme' },
    { value: 'Expired', label: 'Süresi Doldu' },
    { value: 'Cancelled', label: 'İptal' },
  ];

  constructor(private superAdminService: SuperAdminService) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.isLoading = true;
    this.errorMessage = '';
    this.superAdminService.getSubscriptions(this.selectedStatus || undefined).subscribe({
      next: (res) => {
        if (res.success) {
          this.subscriptions = res.data;
        }
        this.isLoading = false;
      },
      error: () => {
        this.errorMessage = 'Abonelikler yüklenemedi.';
        this.isLoading = false;
      }
    });
  }

  get filtered(): SuperAdminSubscription[] {
    if (!this.searchQuery.trim()) return this.subscriptions;
    const q = this.searchQuery.toLowerCase();
    return this.subscriptions.filter(s =>
      s.tenantName?.toLowerCase().includes(q) ||
      s.adminEmail?.toLowerCase().includes(q) ||
      s.plan.toLowerCase().includes(q)
    );
  }

  get totalRevenue(): number {
    return this.subscriptions
      .filter(s => s.status === 'Active')
      .reduce((sum, s) => sum + s.price, 0);
  }

  get activeCount(): number { return this.subscriptions.filter(s => s.status === 'Active').length; }
  get trialCount(): number  { return this.subscriptions.filter(s => s.status === 'Trial').length; }
  get expiredCount(): number { return this.subscriptions.filter(s => s.isExpired).length; }

  statusClass(status: string): string {
    const map: Record<string, string> = {
      Active: 'active', Trial: 'trial', Expired: 'expired', Cancelled: 'cancelled'
    };
    return map[status] ?? 'none';
  }

  statusLabel(status: string): string {
    const map: Record<string, string> = {
      Active: 'Aktif', Trial: 'Deneme', Expired: 'Süresi Doldu', Cancelled: 'İptal'
    };
    return map[status] ?? status;
  }

  formatDate(d: string): string {
    if (!d) return '—';
    return new Date(d).toLocaleDateString('tr-TR');
  }

  formatPrice(price: number, currency: string): string {
    if (price === 0) return 'Ücretsiz';
    return price.toLocaleString('tr-TR') + ' ' + currency;
  }
}
