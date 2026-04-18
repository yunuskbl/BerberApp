import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ServiceService } from '../../../core/services/service.service';
import { Service } from '../../../core/models/service.model';

@Component({
  selector: 'app-service-list',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './service-list.component.html',
  styleUrl: './service-list.component.scss'
})
export class ServiceListComponent implements OnInit {
  serviceList: Service[] = [];
  isLoading       = true;
  isDrawerOpen    = false;
  isSubmitting    = false;
  editingService: Service | null = null;
  errorMessage    = '';

  serviceForm: FormGroup;

  constructor(
    private serviceService: ServiceService,
    private fb: FormBuilder
  ) {
    this.serviceForm = this.fb.group({
      name:            ['', [Validators.required, Validators.maxLength(100)]],
      durationMinutes: [30,  [Validators.required, Validators.min(1), Validators.max(480)]],
      price:           [0,   [Validators.required, Validators.min(0)]],
      currency:        ['TRY', Validators.required],
      color:           ['#7c3aed'],
      isActive:        [true]
    });
  }

  ngOnInit(): void {
    this.loadServices();
  }

  loadServices(): void {
    this.isLoading = true;
    this.serviceService.getAll().subscribe({
      next: (res) => {
        if (res.success) this.serviceList = res.data;
        this.isLoading = false;
      },
      error: () => { this.isLoading = false; }
    });
  }

  openDrawer(service?: Service): void {
    this.editingService = service || null;
    this.errorMessage   = '';

    if (service) {
      this.serviceForm.patchValue(service);
    } else {
      this.serviceForm.reset({
        durationMinutes: 30,
        price:           0,
        currency:        'TRY',
        color:           '#7c3aed',
        isActive:        true
      });
    }

    this.isDrawerOpen = true;
  }

  closeDrawer(): void {
    this.isDrawerOpen   = false;
    this.editingService = null;
  }

  onSubmit(): void {
    if (this.serviceForm.invalid) return;

    this.isSubmitting = true;
    this.errorMessage = '';
    const value = this.serviceForm.value;

    if (this.editingService) {
      this.serviceService.update(this.editingService.id, value).subscribe({
        next: (res) => {
          if (res.success) { this.loadServices(); this.closeDrawer(); }
          this.isSubmitting = false;
        },
        error: (err) => {
          this.errorMessage = err.error?.message || 'Hata oluştu.';
          this.isSubmitting = false;
        }
      });
    } else {
      this.serviceService.create(value).subscribe({
        next: (res) => {
          if (res.success) { this.loadServices(); this.closeDrawer(); }
          this.isSubmitting = false;
        },
        error: (err) => {
          this.errorMessage = err.error?.message || 'Hata oluştu.';
          this.isSubmitting = false;
        }
      });
    }
  }

  deleteService(id: string): void {
    if (!confirm('Bu hizmeti silmek istediğinizden emin misiniz?')) return;
    this.serviceService.delete(id).subscribe({
      next: () => this.loadServices()
    });
  }

  formatPrice(price: number, currency: string): string {
    return new Intl.NumberFormat('tr-TR', {
      style: 'currency',
      currency: currency || 'TRY'
    }).format(price);
  }

  formatDuration(minutes: number): string {
    if (minutes < 60) return `${minutes} dk`;
    const h = Math.floor(minutes / 60);
    const m = minutes % 60;
    return m > 0 ? `${h} sa ${m} dk` : `${h} sa`;
  }
}