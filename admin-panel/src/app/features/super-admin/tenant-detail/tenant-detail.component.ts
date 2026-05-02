import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { SuperAdminService } from '../../../core/services/superadmin.service';

@Component({
  selector: 'app-tenant-detail',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule],
  templateUrl: './tenant-detail.component.html',
  styleUrl: './tenant-detail.component.scss'
})
export class TenantDetailComponent implements OnInit {
  tenant: any = null;
  isLoading = true;
  errorMessage = '';
  successMessage = '';

  showPlanModal = false;
  showDeleteModal = false;
  deleteType: 'soft' | 'hard' = 'soft';
  selectedPlan = '';
  isProcessing = false;

  plans = ['Basic', 'Standard', 'Full'];

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private superAdminService: SuperAdminService
  ) {}

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id')!;
    this.loadDetail(id);
  }

  loadDetail(id: string): void {
    this.isLoading = true;
    this.superAdminService.getTenantDetail(id).subscribe({
      next: (res) => {
        if (res.success) this.tenant = res.data;
        this.selectedPlan = this.tenant?.plan || 'Basic';
        this.isLoading = false;
      },
      error: () => {
        this.errorMessage = 'İşletme yüklenemedi.';
        this.isLoading = false;
      }
    });
  }

  toggleActive(): void {
    this.superAdminService.toggleTenantActive(this.tenant.id).subscribe({
      next: (res) => {
        this.tenant.isActive = res.data.isActive;
        this.showSuccess(res.message || 'Durum güncellendi.');
      }
    });
  }

  openPlanModal(): void {
    this.selectedPlan = this.tenant?.plan || 'Basic';
    this.showPlanModal = true;
  }

  savePlan(): void {
    this.isProcessing = true;
    this.superAdminService.changePlan(this.tenant.id, this.selectedPlan).subscribe({
      next: () => {
        this.tenant.plan = this.selectedPlan;
        this.showPlanModal = false;
        this.isProcessing = false;
        this.showSuccess('Plan güncellendi.');
      },
      error: () => { this.isProcessing = false; }
    });
  }

  openDeleteModal(type: 'soft' | 'hard'): void {
    this.deleteType = type;
    this.showDeleteModal = true;
  }

  confirmDelete(): void {
    this.isProcessing = true;
    this.errorMessage = '';
    const action = this.deleteType === 'soft'
      ? this.superAdminService.softDeleteTenant(this.tenant.id)
      : this.superAdminService.hardDeleteTenant(this.tenant.id);

    action.subscribe({
      next: (res) => {
        this.showDeleteModal = false;
        this.isProcessing = false;
        this.showSuccess(res.message || 'İşletme silindi.');
        setTimeout(() => this.router.navigate(['/superadmin/tenants']), 2000);
      },
      error: (err) => {
        this.errorMessage = err.error?.message || 'Silme işlemi başarısız oldu. Lütfen daha sonra tekrar deneyin.';
        this.isProcessing = false;
        this.showDeleteModal = false;
      }
    });
  }

  formatDate(d: string): string {
    return new Date(d).toLocaleDateString('tr-TR', { day: 'numeric', month: 'long', year: 'numeric' });
  }

  formatTime(d: string): string {
    return new Date(d).toLocaleString('tr-TR', { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit' });
  }

  statusLabel(status: number): string {
    const map: Record<number, string> = { 0: 'Bekliyor', 1: 'Onaylandı', 2: 'Tamamlandı', 3: 'İptal' };
    return map[status] ?? '—';
  }

  statusClass(status: number): string {
    const map: Record<number, string> = { 0: 'pending', 1: 'confirmed', 2: 'completed', 3: 'cancelled' };
    return map[status] ?? '';
  }

  private showSuccess(msg: string): void {
    this.successMessage = msg;
    setTimeout(() => this.successMessage = '', 3000);
  }
}
