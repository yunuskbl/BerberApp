import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { SuperAdminService, AuditLogEntry } from '../../../core/services/superadmin.service';

@Component({
  selector: 'app-super-admin-audit-logs',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './super-admin-audit-logs.component.html',
  styleUrl: './super-admin-audit-logs.component.scss',
})
export class SuperAdminAuditLogsComponent implements OnInit, OnDestroy {

  logs: AuditLogEntry[] = [];
  isLoading = true;
  errorMessage = '';

  total     = 0;
  page      = 1;
  pageSize  = 50;

  selectedSeverity  = '';
  selectedEventType = '';
  fromDate = '';
  toDate   = '';

  readonly severities = [
    { value: '',         label: 'Tümü' },
    { value: 'Critical', label: '🔴 Kritik' },
    { value: 'Warning',  label: '🟡 Uyarı' },
    { value: 'Info',     label: '🟢 Bilgi' },
  ];

  readonly eventTypes = [
    { value: '',                label: 'Tüm Olaylar' },
    { value: 'LOGIN_SUCCESS',   label: 'Başarılı Giriş' },
    { value: 'LOGIN_FAILED',    label: 'Başarısız Giriş' },
    { value: 'UNAUTHORIZED',    label: 'Yetkisiz Erişim' },
    { value: 'FORBIDDEN',       label: 'Yasak Erişim' },
    { value: 'RATE_LIMITED',    label: 'Rate Limit' },
    { value: 'SUPERADMIN_ACTION', label: 'SuperAdmin İşlemi' },
  ];

  private refreshTimer: any;

  constructor(private svc: SuperAdminService) {}

  ngOnInit(): void {
    this.load();
    this.refreshTimer = setInterval(() => this.load(false), 30_000);
  }

  ngOnDestroy(): void {
    clearInterval(this.refreshTimer);
  }

  load(showLoading = true): void {
    if (showLoading) this.isLoading = true;
    this.svc.getAuditLogs({
      page: this.page,
      pageSize: this.pageSize,
      severity:  this.selectedSeverity  || undefined,
      eventType: this.selectedEventType || undefined,
      from: this.fromDate || undefined,
      to:   this.toDate   || undefined,
    }).subscribe({
      next: res => {
        this.logs  = res.data.items;
        this.total = res.data.total;
        this.isLoading = false;
      },
      error: () => {
        this.errorMessage = 'Loglar yüklenemedi.';
        this.isLoading = false;
      },
    });
  }

  applyFilters(): void {
    this.page = 1;
    this.load();
  }

  clearFilters(): void {
    this.selectedSeverity  = '';
    this.selectedEventType = '';
    this.fromDate = '';
    this.toDate   = '';
    this.page     = 1;
    this.load();
  }

  get totalPages(): number { return Math.ceil(this.total / this.pageSize); }

  prevPage(): void { if (this.page > 1) { this.page--; this.load(); } }
  nextPage(): void { if (this.page < this.totalPages) { this.page++; this.load(); } }

  severityClass(s: string): string {
    return s === 'Critical' ? 'badge-critical'
         : s === 'Warning'  ? 'badge-warning'
         :                    'badge-info';
  }

  eventIcon(e: string): string {
    const map: Record<string, string> = {
      LOGIN_SUCCESS:    '✅',
      LOGIN_FAILED:     '❌',
      UNAUTHORIZED:     '🔒',
      FORBIDDEN:        '⛔',
      RATE_LIMITED:     '⚡',
      SUPERADMIN_ACTION:'🛡️',
    };
    return map[e] ?? '📋';
  }

  formatDate(iso: string): string {
    const d = new Date(iso);
    return d.toLocaleString('tr-TR', {
      day: '2-digit', month: '2-digit', year: 'numeric',
      hour: '2-digit', minute: '2-digit', second: '2-digit',
    });
  }
}
