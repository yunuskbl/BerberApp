import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { CustomerService } from '../../../core/services/customer.service';
import { Customer } from '../../../core/models/customer.model';
import { LanguageService } from '../../../core/services/language.service';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';

@Component({
  selector: 'app-customer-list',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, TranslatePipe],
  templateUrl: './customer-list.component.html',
  styleUrl: './customer-list.component.scss'
})
export class CustomerListComponent implements OnInit {
  customerList: Customer[] = [];
  filteredList: Customer[] = [];
  isLoading        = true;
  isDrawerOpen     = false;
  isSubmitting     = false;
  editingCustomer: Customer | null = null;
  errorMessage     = '';
  searchQuery      = '';

  customerForm: FormGroup;

  constructor(
    private customerService: CustomerService,
    private fb: FormBuilder,
    public langService: LanguageService,
  ) {
    this.customerForm = this.fb.group({
      fullName: ['', [Validators.required, Validators.maxLength(100)]],
      phone:    ['', [Validators.required]],
      email:    ['', [Validators.email]],
      notes:    ['']
    });
  }

  ngOnInit(): void {
    this.loadCustomers();
  }

  loadCustomers(): void {
    this.isLoading = true;
    this.customerService.getAll().subscribe({
      next: (res) => {
        if (res.success) { this.customerList = res.data; this.filteredList = res.data; }
        this.isLoading = false;
      },
      error: () => { this.isLoading = false; }
    });
  }

  onSearch(event: Event): void {
    const query = (event.target as HTMLInputElement).value.toLowerCase();
    this.searchQuery  = query;
    this.filteredList = this.customerList.filter(c =>
      c.fullName.toLowerCase().includes(query) ||
      c.phone.includes(query) ||
      (c.email?.toLowerCase().includes(query) ?? false)
    );
  }

  openDrawer(customer?: Customer): void {
    this.editingCustomer = customer || null;
    this.errorMessage    = '';
    if (customer) {
      this.customerForm.patchValue(customer);
    } else {
      this.customerForm.reset();
    }
    this.isDrawerOpen = true;
  }

  closeDrawer(): void {
    this.isDrawerOpen    = false;
    this.editingCustomer = null;
    this.customerForm.reset();
  }

  onSubmit(): void {
    if (this.customerForm.invalid) return;
    this.isSubmitting = true;
    this.errorMessage = '';
    const value = this.customerForm.value;

    if (this.editingCustomer) {
      this.customerService.update(this.editingCustomer.id, value).subscribe({
        next: (res) => {
          if (res.success) { this.loadCustomers(); this.closeDrawer(); }
          this.isSubmitting = false;
        },
        error: (err) => { this.errorMessage = err.error?.message || 'Hata oluştu.'; this.isSubmitting = false; }
      });
    } else {
      this.customerService.create(value).subscribe({
        next: (res) => {
          if (res.success) { this.loadCustomers(); this.closeDrawer(); }
          this.isSubmitting = false;
        },
        error: (err) => { this.errorMessage = err.error?.message || 'Hata oluştu.'; this.isSubmitting = false; }
      });
    }
  }

  deleteCustomer(id: string): void {
    if (!confirm('?')) return;
    this.customerService.delete(id).subscribe({ next: () => this.loadCustomers() });
  }

  getInitials(name: string): string {
    return name.split(' ').map(n => n[0]).join('').toUpperCase().slice(0, 2);
  }
}