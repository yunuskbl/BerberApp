import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { SuperAdminService, SuperAdminTenant, CreateTenantRequest } from '../../../core/services/superadmin.service';

@Component({
  selector: 'app-super-admin-tenants',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './super-admin-tenants.component.html',
  styleUrl: './super-admin-tenants.component.scss'
})
export class SuperAdminTenantsComponent implements OnInit {
  tenants: SuperAdminTenant[] = [];
  isLoading = true;
  isDrawerOpen = false;
  isSubmitting = false;
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
    this.superAdminService.getAllTenants().subscribe({
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

  get totalTenants(): number {
    return this.tenants.length;
  }

  get activeTenants(): number {
    return this.tenants.filter(t => t.isActive).length;
  }

  get totalAppointments(): number {
    return this.tenants.reduce((sum, t) => sum + t.totalAppointments, 0);
  }

  get totalCustomers(): number {
    return this.tenants.reduce((sum, t) => sum + t.customerCount, 0);
  }

  formatDate(dateStr: string): string {
    return new Date(dateStr).toLocaleDateString('tr-TR');
  }
}
