import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { SuperAdminService, AppointmentStats } from '../../../core/services/superadmin.service';

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

  // Mevcut modaller
  showPlanModal    = false;
  showDeleteModal  = false;
  deleteType: 'soft' | 'hard' = 'soft';
  selectedPlan     = '';
  isProcessing     = false;

  // Yeni modaller
  showEditModal     = false;
  showExtendModal   = false;
  showPasswordModal = false;
  showNotifyModal   = false;

  editForm  = { name: '', phone: '', address: '' };
  extendDays        = 30;
  newPassword       = '';
  notifyMessage     = '';

  // Randevu istatistikleri
  appointmentStats: AppointmentStats | null = null;
  statsLoading = false;

  plans = [
    { value: 'Baslangic',   label: '🌱 Başlangıç'   },
    { value: 'Profesyonel', label: '⚡ Profesyonel' },
    { value: 'Premium',     label: '👑 Premium'      },
  ];

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private superAdminService: SuperAdminService
  ) {}

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id')!;
    this.loadDetail(id);
    this.loadStats(id);
  }

  loadDetail(id: string): void {
    this.isLoading = true;
    this.superAdminService.getTenantDetail(id).subscribe({
      next: (res) => {
        if (res.success) this.tenant = res.data;
        this.selectedPlan = this.tenant?.plan || 'Baslangic';
        this.isLoading = false;
      },
      error: () => { this.errorMessage = 'İşletme yüklenemedi.'; this.isLoading = false; }
    });
  }

  loadStats(id: string): void {
    this.statsLoading = true;
    this.superAdminService.getTenantAppointmentStats(id).subscribe({
      next: (res) => { if (res.success) this.appointmentStats = res.data; this.statsLoading = false; },
      error: () => { this.statsLoading = false; }
    });
  }

  // ─── Mevcut işlemler ────────────────────────────────────────────────────
  toggleActive(): void {
    this.superAdminService.toggleTenantActive(this.tenant.id).subscribe({
      next: (res) => { this.tenant.isActive = res.data.isActive; this.showSuccess(res.message || 'Durum güncellendi.'); }
    });
  }

  openPlanModal(): void { this.selectedPlan = this.tenant?.plan || 'Baslangic'; this.showPlanModal = true; }

  savePlan(): void {
    this.isProcessing = true;
    this.superAdminService.changePlan(this.tenant.id, this.selectedPlan).subscribe({
      next: () => { this.tenant.plan = this.selectedPlan; this.showPlanModal = false; this.isProcessing = false; this.showSuccess('Plan güncellendi.'); },
      error: () => { this.isProcessing = false; }
    });
  }

  openDeleteModal(type: 'soft' | 'hard'): void { this.deleteType = type; this.showDeleteModal = true; }

  confirmDelete(): void {
    this.isProcessing = true;
    this.errorMessage = '';
    const action = this.deleteType === 'soft'
      ? this.superAdminService.softDeleteTenant(this.tenant.id)
      : this.superAdminService.hardDeleteTenant(this.tenant.id);

    action.subscribe({
      next: (res) => {
        this.showDeleteModal = false; this.isProcessing = false;
        this.showSuccess(res.message || 'İşletme silindi.');
        setTimeout(() => this.router.navigate(['/superadmin/tenants']), 2000);
      },
      error: (err) => {
        this.errorMessage = err.error?.message || 'Silme işlemi başarısız.';
        this.isProcessing = false; this.showDeleteModal = false;
      }
    });
  }

  // ─── Yeni: İşletme düzenleme ────────────────────────────────────────────
  openEditModal(): void {
    this.editForm = { name: this.tenant.name, phone: this.tenant.phone || '', address: this.tenant.address || '' };
    this.showEditModal = true;
  }

  saveEdit(): void {
    this.isProcessing = true;
    this.superAdminService.updateTenant(this.tenant.id, this.editForm).subscribe({
      next: (res) => {
        Object.assign(this.tenant, this.editForm);
        this.showEditModal = false; this.isProcessing = false;
        this.showSuccess(res.message || 'İşletme güncellendi.');
      },
      error: () => { this.isProcessing = false; }
    });
  }

  // ─── Yeni: Abonelik uzatma ───────────────────────────────────────────────
  saveExtend(): void {
    this.isProcessing = true;
    this.superAdminService.extendSubscription(this.tenant.id, this.extendDays).subscribe({
      next: (res) => {
        this.showExtendModal = false; this.isProcessing = false;
        this.showSuccess(res.message || `Abonelik ${this.extendDays} gün uzatıldı.`);
      },
      error: () => { this.isProcessing = false; }
    });
  }

  // ─── Yeni: Şifre sıfırlama ──────────────────────────────────────────────
  savePassword(): void {
    if (this.newPassword.length < 6) { this.showSuccess('Şifre en az 6 karakter olmalıdır.'); return; }
    this.isProcessing = true;
    this.superAdminService.resetAdminPassword(this.tenant.id, this.newPassword).subscribe({
      next: (res) => {
        this.showPasswordModal = false; this.newPassword = ''; this.isProcessing = false;
        this.showSuccess(res.message || 'Şifre sıfırlandı.');
      },
      error: () => { this.isProcessing = false; }
    });
  }

  // ─── Yeni: Bildirim gönder ──────────────────────────────────────────────
  sendNotify(): void {
    if (!this.notifyMessage.trim()) return;
    this.isProcessing = true;
    this.superAdminService.notifyTenant(this.tenant.id, this.notifyMessage).subscribe({
      next: (res) => {
        this.showNotifyModal = false; this.notifyMessage = ''; this.isProcessing = false;
        this.showSuccess(res.message || 'Bildirim gönderildi.');
      },
      error: () => { this.isProcessing = false; }
    });
  }

  // ─── Yardımcılar ────────────────────────────────────────────────────────
  getMonthName(month: number): string {
    return ['Oca','Şub','Mar','Nis','May','Haz','Tem','Ağu','Eyl','Eki','Kas','Ara'][month - 1] ?? '';
  }

  get maxMonthlyCount(): number {
    if (!this.appointmentStats?.monthly?.length) return 1;
    return Math.max(...this.appointmentStats.monthly.map(m => m.total), 1);
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
    setTimeout(() => this.successMessage = '', 3500);
  }
}
