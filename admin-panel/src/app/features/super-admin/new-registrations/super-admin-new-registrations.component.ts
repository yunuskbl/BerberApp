import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { SuperAdminService, SuperAdminTenant } from '../../../core/services/superadmin.service';

interface FlaggedTenant extends SuperAdminTenant {
  score: number;
  reasons: string[];
}

@Component({
  selector: 'app-super-admin-new-registrations',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './super-admin-new-registrations.component.html',
  styleUrl: './super-admin-new-registrations.component.scss',
})
export class SuperAdminNewRegistrationsComponent implements OnInit {
  all: SuperAdminTenant[] = [];
  isLoading = true;
  errorMessage = '';
  successMessage = '';
  busyId: string | null = null;

  // Filtre: kaç gün geriye bakılacak
  windowDays = 7;
  windowOptions = [
    { value: 1,  label: 'Son 24 saat' },
    { value: 7,  label: 'Son 7 gün' },
    { value: 30, label: 'Son 30 gün' },
    { value: 0,  label: 'Tümü' },
  ];
  onlySuspicious = false;

  constructor(private superAdmin: SuperAdminService) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.isLoading = true;
    this.superAdmin.getAllTenants(false).subscribe({
      next: (res) => {
        this.all = res.success ? res.data : [];
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
        this.errorMessage = 'Kayıtlar yüklenemedi.';
      },
    });
  }

  // ── Şüphe puanı hesaplama ────────────────────────────────────────────────
  private evaluate(t: SuperAdminTenant): FlaggedTenant {
    const reasons: string[] = [];
    let score = 0;

    const ageHours = (Date.now() - new Date(t.createdAt).getTime()) / 36e5;

    // Hiç aktivite yok (personel + müşteri + randevu = 0)
    if (t.staffCount === 0 && t.customerCount === 0 && t.totalAppointments === 0) {
      score += 2;
      reasons.push('Hiç aktivite yok');
    }
    // Çok yeni ve boş
    if (ageHours < 1) {
      score += 1;
      reasons.push('1 saatten yeni');
    }
    // İşletme adı şüpheli (rastgele/anlamsız görünen)
    if (this.looksGibberish(t.name)) {
      score += 2;
      reasons.push('Şüpheli işletme adı');
    }
    // E-posta yerel kısmı şüpheli
    if (t.adminEmail && this.looksGibberish(t.adminEmail.split('@')[0])) {
      score += 2;
      reasons.push('Şüpheli e-posta');
    }
    // Telefon eksik
    if (!t.phone || t.phone.replace(/\D/g, '').length < 10) {
      score += 1;
      reasons.push('Geçersiz/eksik telefon');
    }

    return { ...t, score, reasons };
  }

  /** Basit rastgele-karakter sezgisi: sesli harf yok ya da 5+ ardışık sessiz harf */
  private looksGibberish(text: string): boolean {
    if (!text) return false;
    const s = text.toLowerCase().replace(/[^a-zçğıöşü]/g, '');
    if (s.length < 4) return false;
    if (!/[aeıioöuü]/.test(s)) return true;
    if (/[bcdfghjklmnpqrstvwxyzçğş]{5,}/.test(s)) return true;
    return false;
  }

  get list(): FlaggedTenant[] {
    const cutoff = this.windowDays > 0
      ? Date.now() - this.windowDays * 24 * 36e5
      : 0;

    let items = this.all
      .filter(t => new Date(t.createdAt).getTime() >= cutoff)
      .map(t => this.evaluate(t))
      .sort((a, b) => {
        // Önce şüphe puanı, sonra en yeni
        if (b.score !== a.score) return b.score - a.score;
        return new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime();
      });

    if (this.onlySuspicious) items = items.filter(t => t.score >= 3);
    return items;
  }

  get suspiciousCount(): number {
    return this.list.filter(t => t.score >= 3).length;
  }

  // ── Aksiyonlar ───────────────────────────────────────────────────────────
  approve(t: FlaggedTenant): void {
    if (!confirm(`"${t.name}" işletmesini ONAYLA?\n\nOnaylandıktan sonra salonlar listesinde yayına çıkar.`)) return;
    this.busyId = t.id;
    this.superAdmin.approveTenant(t.id, true).subscribe({
      next: () => { this.flash(`"${t.name}" onaylandı, salonlarda yayında.`); this.load(); this.busyId = null; },
      error: () => { this.fail('Onaylama başarısız.'); this.busyId = null; },
    });
  }

  unapprove(t: FlaggedTenant): void {
    if (!confirm(`"${t.name}" işletmesinin onayını KALDIR?\n\nSalonlar listesinden çıkar.`)) return;
    this.busyId = t.id;
    this.superAdmin.approveTenant(t.id, false).subscribe({
      next: () => { this.flash(`"${t.name}" onayı kaldırıldı.`); this.load(); this.busyId = null; },
      error: () => { this.fail('İşlem başarısız.'); this.busyId = null; },
    });
  }

  toggleActive(t: FlaggedTenant): void {
    if (!confirm(`"${t.name}" işletmesini ${t.isActive ? 'ASKIYA AL' : 'AKTİF ET'}?`)) return;
    this.busyId = t.id;
    this.superAdmin.toggleTenantActive(t.id).subscribe({
      next: () => { this.flash(`"${t.name}" ${t.isActive ? 'askıya alındı' : 'aktifleştirildi'}.`); this.load(); this.busyId = null; },
      error: () => { this.fail('İşlem başarısız.'); this.busyId = null; },
    });
  }

  softDelete(t: FlaggedTenant): void {
    if (!confirm(`"${t.name}" işletmesini SİL (çöp kutusu)?\n\nGeri yüklenebilir.`)) return;
    this.busyId = t.id;
    this.superAdmin.softDeleteTenant(t.id).subscribe({
      next: () => { this.flash(`"${t.name}" silindi.`); this.load(); this.busyId = null; },
      error: () => { this.fail('Silme başarısız.'); this.busyId = null; },
    });
  }

  hardDelete(t: FlaggedTenant): void {
    if (!confirm(`"${t.name}" işletmesini KALICI OLARAK SİL?\n\n⚠️ Bu işlem GERİ ALINAMAZ! Tüm veriler kalıcı olarak silinir.`)) return;
    if (!confirm(`Emin misiniz? "${t.name}" ve tüm verileri kalıcı silinecek.`)) return;
    this.busyId = t.id;
    this.superAdmin.hardDeleteTenant(t.id).subscribe({
      next: () => { this.flash(`"${t.name}" kalıcı olarak silindi.`); this.load(); this.busyId = null; },
      error: (err) => { this.fail(err.error?.message || 'Kalıcı silme başarısız.'); this.busyId = null; },
    });
  }

  private flash(msg: string): void {
    this.successMessage = msg;
    setTimeout(() => this.successMessage = '', 4000);
  }
  private fail(msg: string): void {
    this.errorMessage = msg;
    setTimeout(() => this.errorMessage = '', 4000);
  }

  // ── Yardımcılar ──────────────────────────────────────────────────────────
  formatDateTime(dateStr: string): string {
    if (!dateStr) return '—';
    const d = new Date(dateStr);
    return d.toLocaleDateString('tr-TR') + ' ' + d.toLocaleTimeString('tr-TR', { hour: '2-digit', minute: '2-digit' });
  }

  ageLabel(dateStr: string): string {
    const mins = (Date.now() - new Date(dateStr).getTime()) / 6e4;
    if (mins < 60) return `${Math.floor(mins)} dk önce`;
    const hrs = mins / 60;
    if (hrs < 24) return `${Math.floor(hrs)} saat önce`;
    return `${Math.floor(hrs / 24)} gün önce`;
  }
}
