import { Component, OnInit, computed, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { PackageService } from '../../../core/services/package.service';
import { CustomerService } from '../../../core/services/customer.service';
import { Customer } from '../../../core/models/customer.model';
import {
  PAYMENT_METHOD_LABELS, PackageDetail, PackagePaymentMethod, PackageTemplate,
} from '../../../core/models/package.model';
import { money } from '../package-format';

/** Müşteriye paket satışı. Tanım seçilince değerler dolar; hepsi değiştirilebilir. */
@Component({
  selector: 'app-sell-package',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './sell-package.component.html',
  styleUrl: './sell-package.component.scss',
})
export class SellPackageComponent implements OnInit {
  readonly templates = input<PackageTemplate[]>([]);
  readonly sold      = output<PackageDetail>();
  readonly cancelled = output<void>();

  readonly customers = signal<Customer[]>([]);
  readonly saving    = signal(false);
  readonly error     = signal('');

  customerQuery = '';
  customer: Customer | null = null;

  templateId = '';
  name = '';
  totalSessions: number | null = null;
  price: number | null = null;
  validityDays: number | null = null;
  note = '';
  paymentAmount: number | null = null;
  paymentMethod: PackagePaymentMethod = 'Cash';

  readonly methods = Object.entries(PAYMENT_METHOD_LABELS) as [PackagePaymentMethod, string][];
  readonly activeTemplates = computed(() => this.templates().filter(t => t.isActive));
  readonly money = money;

  constructor(private packages: PackageService, private customerService: CustomerService) {}

  ngOnInit(): void {
    this.customerService.getAll().subscribe({
      next: r => { if (r.success) this.customers.set(r.data ?? []); },
      error: () => {},
    });
  }

  get matches(): Customer[] {
    const q = this.customerQuery.trim().toLocaleLowerCase('tr-TR');
    if (!q) return [];
    const digits = q.replace(/\D/g, '');
    return this.customers()
      .filter(c => c.fullName.toLocaleLowerCase('tr-TR').includes(q) || (digits.length >= 3 && c.phone.replace(/\D/g, '').includes(digits)))
      .slice(0, 8);
  }

  pickCustomer(c: Customer): void {
    this.customer = c;
    this.customerQuery = '';
  }

  onTemplateChange(): void {
    const t = this.templates().find(x => x.id === this.templateId);
    if (!t) return;
    this.name = t.name;
    this.totalSessions = t.sessionCount;
    this.price = t.price;
    this.validityDays = t.validityDays ?? null;
  }

  get remainingAfterPayment(): number | null {
    if (this.price == null) return null;
    return Math.max(0, Math.round((this.price - (this.paymentAmount || 0)) * 100) / 100);
  }

  submit(): void {
    if (this.saving()) return;
    this.error.set('');
    if (!this.customer) { this.error.set('Müşteri seçin.'); return; }
    if (!this.name.trim()) { this.error.set('Paket adı gerekli.'); return; }
    if (!this.totalSessions || this.totalSessions < 1 || !Number.isInteger(this.totalSessions)) { this.error.set('Seans sayısı en az 1 olmalı.'); return; }
    if (this.price == null || this.price < 0) { this.error.set('Paket fiyatını girin.'); return; }
    if (this.paymentAmount != null && this.paymentAmount < 0) { this.error.set('Ödeme tutarı geçersiz.'); return; }
    if ((this.paymentAmount || 0) > this.price) { this.error.set('Alınan ödeme paket fiyatından fazla olamaz.'); return; }

    this.saving.set(true);
    this.packages.sell({
      customerId: this.customer.id,
      templateId: this.templateId || null,
      name: this.name.trim(),
      totalSessions: this.totalSessions,
      price: this.price,
      validityDays: this.validityDays || null,
      note: this.note.trim() || null,
      initialPayment: this.paymentAmount && this.paymentAmount > 0
        ? { amount: this.paymentAmount, method: this.paymentMethod }
        : null,
    }).subscribe({
      next: r => { this.saving.set(false); if (r.success) this.sold.emit(r.data); },
      error: err => { this.saving.set(false); this.error.set(err?.error?.message || 'Paket satılamadı.'); },
    });
  }
}
