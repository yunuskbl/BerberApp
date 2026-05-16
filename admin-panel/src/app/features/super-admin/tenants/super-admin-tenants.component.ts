import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { SuperAdminService, SuperAdminTenant, CreateTenantRequest } from '../../../core/services/superadmin.service';

@Component({
  selector: 'app-super-admin-tenants',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FormsModule, RouterModule],
  templateUrl: './super-admin-tenants.component.html',
  styleUrl: './super-admin-tenants.component.scss'
})
export class SuperAdminTenantsComponent implements OnInit {
  tenants: SuperAdminTenant[] = [];
  isLoading = true;
  isDrawerOpen = false;
  isSubmitting = false;
  isResetting: string | null = null;
  isRestoring: string | null = null;
  showDeleted = false;
  errorMessage = '';
  successMessage = '';

  createTenantForm: FormGroup;

  constructor(
    private superAdminService: SuperAdminService,
    private fb: FormBuilder,
  ) {
    this.createTenantForm = this.fb.group({
      tenantName: ['', [Validators.required, Validators.maxLength(100)]],
      subdomain: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(50), Validators.pattern(/^[a-z0-9-]+$/)]],
      firstName: ['', [Validators.required, Validators.maxLength(50)]],
      lastName: ['', [Validators.required, Validators.maxLength(50)]],
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(8)]],
      phone: [''],
      address: [''],
    });
  }

  ngOnInit(): void {
    this.loadTenants();
  }

  loadTenants(): void {
    this.isLoading = true;
    this.superAdminService.getAllTenants(this.showDeleted).subscribe({
      next: (res) => {
        if (res.success) {
          this.tenants = res.data;
        }
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
        this.errorMessage = 'Tenant\'lar yüklenemedi.';
      }
    });
  }

  toggleShowDeleted(): void {
    this.showDeleted = !this.showDeleted;
    this.loadTenants();
  }

  openDrawer(): void {
    this.errorMessage = '';
    this.successMessage = '';
    this.createTenantForm.reset();
    this.isDrawerOpen = true;
  }

  closeDrawer(): void {
    this.isDrawerOpen = false;
    this.createTenantForm.reset();
  }

  onSubmit(): void {
    if (this.createTenantForm.invalid) return;

    this.isSubmitting = true;
    this.errorMessage = '';

    const request: CreateTenantRequest = this.createTenantForm.value;

    this.superAdminService.createTenant(request).subscribe({
      next: (res) => {
        if (res.success) {
          this.successMessage = 'İşletme oluşturuldu!';
          this.closeDrawer();
          this.loadTenants();
          setTimeout(() => this.successMessage = '', 3000);
        }
        this.isSubmitting = false;
      },
      error: (err) => {
        this.errorMessage = err.error?.message || 'Hata oluştu.';
        this.isSubmitting = false;
      }
    });
  }

  resetTenantData(tenantId: string, tenantName: string): void {
    if (!confirm(`"${tenantName}" işletmesinin tüm randevu, müşteri, personel ve yorum verilerini sıfırlamak istediğinizden emin misiniz?\n\nBu işlem geri alınamaz!`)) return;

    this.isResetting = tenantId;
    this.errorMessage = '';
    this.successMessage = '';

    this.superAdminService.resetTenantData(tenantId).subscribe({
      next: (res) => {
        if (res.success) {
          this.successMessage = `"${tenantName}" verileri sıfırlandı.`;
          this.loadTenants();
          setTimeout(() => this.successMessage = '', 4000);
        } else {
          this.errorMessage = res.message || 'Sıfırlama işlemi başarısız.';
          setTimeout(() => this.errorMessage = '', 4000);
        }
        this.isResetting = null;
      },
      error: (err) => {
        this.errorMessage = err.error?.message || 'Sıfırlama işlemi başarısız.';
        setTimeout(() => this.errorMessage = '', 4000);
        this.isResetting = null;
      }
    });
  }

  restoreTenant(tenantId: string, tenantName: string): void {
    if (!confirm(`"${tenantName}" işletmesini geri yüklemek istediğinizden emin misiniz?`)) return;

    this.isRestoring = tenantId;
    this.errorMessage = '';
    this.successMessage = '';

    this.superAdminService.restoreTenant(tenantId).subscribe({
      next: (res) => {
        if (res.success) {
          this.successMessage = res.message || `"${tenantName}" geri yüklendi.`;
          this.loadTenants();
          setTimeout(() => this.successMessage = '', 4000);
        } else {
          this.errorMessage = res.message || 'Geri yükleme başarısız.';
          setTimeout(() => this.errorMessage = '', 4000);
        }
        this.isRestoring = null;
      },
      error: (err) => {
        this.errorMessage = err.error?.message || 'Geri yükleme başarısız.';
        setTimeout(() => this.errorMessage = '', 4000);
        this.isRestoring = null;
      }
    });
  }

  toggleTenantActive(tenantId: string, currentStatus: boolean): void {
    if (!confirm(`İşletmeyi ${currentStatus ? 'pasif' : 'aktif'} yapmak istediğinizden emin misiniz?`)) return;

    this.superAdminService.toggleTenantActive(tenantId).subscribe({
      next: () => {
        this.loadTenants();
      },
      error: () => {
        this.errorMessage = 'İşlem başarısız.';
      }
    });
  }

  searchQuery = '';

  get filteredTenants() {
    if (!this.searchQuery.trim()) return this.tenants;
    const q = this.searchQuery.toLowerCase();
    return this.tenants.filter(t =>
      t.name.toLowerCase().includes(q) ||
      t.subdomain.toLowerCase().includes(q) ||
      t.adminEmail?.toLowerCase().includes(q)
    );
  }

  get totalTenants(): number { return this.tenants.length; }
  get activeTenants(): number { return this.tenants.filter(t => t.isActive).length; }
  get trialTenants(): number { return this.tenants.filter(t => t.isOnTrial).length; }
  get totalAppointments(): number { return this.tenants.reduce((s, t) => s + t.totalAppointments, 0); }
  get totalCustomers(): number { return this.tenants.reduce((s, t) => s + t.customerCount, 0); }

  subStatusLabel(t: any): string {
    if (!t.subscriptionStatus || t.subscriptionStatus === 'None') return 'Yok';
    const map: Record<string, string> = {
      Trial: 'Deneme', Active: 'Aktif', Expired: 'Süresi Doldu',
      Cancelled: 'İptal', Inactive: 'Pasif', Pending: 'Bekliyor'
    };
    return map[t.subscriptionStatus] ?? t.subscriptionStatus;
  }

  subStatusClass(t: any): string {
    const map: Record<string, string> = {
      Trial: 'trial', Active: 'active', Expired: 'expired',
      Cancelled: 'cancelled', None: 'none'
    };
    return map[t.subscriptionStatus] ?? 'none';
  }

  formatDate(dateStr: string): string {
    if (!dateStr) return '—';
    return new Date(dateStr).toLocaleDateString('tr-TR');
  }

  formatExpiry(t: any): string {
    if (!t.subscriptionExpiresAt) return '—';
    const d = new Date(t.subscriptionExpiresAt);
    const label = d.toLocaleDateString('tr-TR');
    return t.daysLeft != null ? `${label} (${t.daysLeft}g)` : label;
  }
}
