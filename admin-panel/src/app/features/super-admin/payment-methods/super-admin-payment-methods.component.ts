import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { SuperAdminService, PaymentMethod } from '../../../core/services/superadmin.service';

@Component({
  selector: 'app-super-admin-payment-methods',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './super-admin-payment-methods.component.html',
  styleUrl: './super-admin-payment-methods.component.scss'
})
export class SuperAdminPaymentMethodsComponent implements OnInit {
  methods: PaymentMethod[] = [];
  isLoading = true;
  errorMessage = '';
  successMessage = '';

  showForm = false;
  editingId: string | null = null;

  form: Omit<PaymentMethod, 'id' | 'createdAt'> = this.emptyForm();

  constructor(private svc: SuperAdminService) {}

  ngOnInit(): void { this.load(); }

  load(): void {
    this.isLoading = true;
    this.svc.getPaymentMethods().subscribe({
      next: res => { if (res.success) this.methods = res.data; this.isLoading = false; },
      error: () => { this.errorMessage = 'Yüklenemedi.'; this.isLoading = false; }
    });
  }

  openAdd(): void {
    this.editingId = null;
    this.form = this.emptyForm();
    this.showForm = true;
  }

  openEdit(m: PaymentMethod): void {
    this.editingId = m.id;
    this.form = { name: m.name, bankName: m.bankName, iban: m.iban, accountHolder: m.accountHolder, description: m.description, isActive: m.isActive, order: m.order };
    this.showForm = true;
  }

  save(): void {
    this.successMessage = '';
    this.errorMessage = '';
    const obs = this.editingId
      ? this.svc.updatePaymentMethod(this.editingId, this.form)
      : this.svc.createPaymentMethod(this.form);

    obs.subscribe({
      next: res => {
        if (res.success) {
          this.successMessage = this.editingId ? 'Güncellendi.' : 'Eklendi.';
          this.showForm = false;
          this.load();
        }
      },
      error: () => { this.errorMessage = 'İşlem başarısız.'; }
    });
  }

  delete(id: string): void {
    if (!confirm('Silmek istediğinize emin misiniz?')) return;
    this.svc.deletePaymentMethod(id).subscribe({
      next: () => { this.successMessage = 'Silindi.'; this.load(); },
      error: () => { this.errorMessage = 'Silme başarısız.'; }
    });
  }

  cancel(): void { this.showForm = false; }

  private emptyForm(): Omit<PaymentMethod, 'id' | 'createdAt'> {
    return { name: '', bankName: '', iban: '', accountHolder: '', description: '', isActive: true, order: 0 };
  }
}
