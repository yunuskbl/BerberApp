import { Component, OnInit, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { PackageService } from '../../../core/services/package.service';
import { ServiceService } from '../../../core/services/service.service';
import { PackageTemplate } from '../../../core/models/package.model';
import { Service } from '../../../core/models/service.model';
import { money } from '../package-format';

interface TemplateForm {
  id: string | null;
  name: string;
  serviceId: string;
  sessionCount: number | null;
  price: number | null;
  validityDays: number | null;
  isActive: boolean;
}

/** Paket tanımları: işletmenin sattığı hazır paketler (ör. "Lazer — 8 seans"). */
@Component({
  selector: 'app-package-templates',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './package-templates.component.html',
  styleUrl: './package-templates.component.scss',
})
export class PackageTemplatesComponent implements OnInit {
  /** Tanımlar değişti; satış formu güncel listeyi kullansın. */
  readonly changed = output<PackageTemplate[]>();

  readonly templates = signal<PackageTemplate[]>([]);
  readonly services  = signal<Service[]>([]);
  readonly loading   = signal(true);
  readonly saving    = signal(false);
  readonly error     = signal('');

  form: TemplateForm | null = null;
  readonly money = money;

  constructor(private packages: PackageService, private serviceService: ServiceService) {}

  ngOnInit(): void {
    this.load();
    this.serviceService.getAll().subscribe({
      next: r => { if (r.success) this.services.set((r.data ?? []).filter(s => s.isActive)); },
      error: () => {},
    });
  }

  load(): void {
    this.loading.set(true);
    this.packages.getTemplates().subscribe({
      next: r => {
        this.loading.set(false);
        if (r.success) { this.templates.set(r.data ?? []); this.changed.emit(r.data ?? []); }
      },
      error: () => this.loading.set(false),
    });
  }

  openNew(): void {
    this.error.set('');
    this.form = { id: null, name: '', serviceId: '', sessionCount: null, price: null, validityDays: null, isActive: true };
  }

  openEdit(t: PackageTemplate): void {
    this.error.set('');
    this.form = {
      id: t.id, name: t.name, serviceId: t.serviceId ?? '', sessionCount: t.sessionCount,
      price: t.price, validityDays: t.validityDays ?? null, isActive: t.isActive,
    };
  }

  /** Hizmet seçilince ad boşsa hizmet adını öner. */
  onServiceChange(): void {
    if (!this.form || this.form.name.trim()) return;
    const s = this.services().find(x => x.id === this.form!.serviceId);
    if (s) this.form.name = s.name;
  }

  close(): void { this.form = null; }

  save(): void {
    const f = this.form;
    if (!f || this.saving()) return;
    if (!f.name.trim()) { this.error.set('Paket adı gerekli.'); return; }
    if (!f.sessionCount || f.sessionCount < 1 || !Number.isInteger(f.sessionCount)) { this.error.set('Seans sayısı en az 1 olmalı.'); return; }
    if (f.price == null || f.price < 0) { this.error.set('Fiyat girin.'); return; }

    const req = {
      name: f.name.trim(),
      serviceId: f.serviceId || null,
      sessionCount: f.sessionCount,
      price: f.price,
      validityDays: f.validityDays || null,
      isActive: f.isActive,
    };
    this.saving.set(true);
    this.error.set('');
    const call = f.id ? this.packages.updateTemplate(f.id, req) : this.packages.createTemplate(req);
    call.subscribe({
      next: () => { this.saving.set(false); this.form = null; this.load(); },
      error: err => { this.saving.set(false); this.error.set(err?.error?.message || 'Kaydedilemedi.'); },
    });
  }

  remove(t: PackageTemplate): void {
    if (!confirm(`"${t.name}" tanımı silinsin mi? Satılmış paketler etkilenmez.`)) return;
    this.packages.deleteTemplate(t.id).subscribe({
      next: () => this.load(),
      error: err => alert(err?.error?.message || 'Silinemedi.'),
    });
  }

  validityLabel(days?: number | null): string {
    if (!days) return 'Süresiz';
    if (days % 30 === 0) return `${days / 30} ay geçerli`;
    return `${days} gün geçerli`;
  }
}
