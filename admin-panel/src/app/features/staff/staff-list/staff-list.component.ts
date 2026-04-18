import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { StaffService } from '../../../core/services/staff.service';
import { Staff } from '../../../core/models/staff.model';

@Component({
  selector: 'app-staff-list',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './staff-list.component.html',
  styleUrl: './staff-list.component.scss'
})
export class StaffListComponent implements OnInit {
  staffList: Staff[] = [];
  isLoading          = true;
  isDrawerOpen       = false;
  isSubmitting       = false;
  editingStaff: Staff | null = null;
  errorMessage       = '';

  staffForm: FormGroup;

  constructor(
    private staffService: StaffService,
    private fb: FormBuilder
  ) {
    this.staffForm = this.fb.group({
      fullName:  ['', [Validators.required, Validators.maxLength(100)]],
      phone:     [''],
      avatarUrl: [''],
      bio:       [''],
      isActive:  [true]
    });
  }

  ngOnInit(): void {
    this.loadStaff();
  }

  loadStaff(): void {
    this.isLoading = true;
    this.staffService.getAll().subscribe({
      next: (res) => {
        if (res.success) this.staffList = res.data;
        this.isLoading = false;
      },
      error: () => { this.isLoading = false; }
    });
  }

  openDrawer(staff?: Staff): void {
    this.editingStaff = staff || null;
    this.errorMessage = '';

    if (staff) {
      this.staffForm.patchValue(staff);
    } else {
      this.staffForm.reset({ isActive: true });
    }

    this.isDrawerOpen = true;
  }

  closeDrawer(): void {
    this.isDrawerOpen = false;
    this.editingStaff = null;
    this.staffForm.reset({ isActive: true });
  }

  onSubmit(): void {
    if (this.staffForm.invalid) return;

    this.isSubmitting = true;
    this.errorMessage = '';

    const value = this.staffForm.value;

    if (this.editingStaff) {
      this.staffService.update(this.editingStaff.id, value).subscribe({
        next: (res) => {
          if (res.success) {
            this.loadStaff();
            this.closeDrawer();
          }
          this.isSubmitting = false;
        },
        error: (err) => {
          this.errorMessage = err.error?.message || 'Hata oluştu.';
          this.isSubmitting = false;
        }
      });
    } else {
      this.staffService.create(value).subscribe({
        next: (res) => {
          if (res.success) {
            this.loadStaff();
            this.closeDrawer();
          }
          this.isSubmitting = false;
        },
        error: (err) => {
          this.errorMessage = err.error?.message || 'Hata oluştu.';
          this.isSubmitting = false;
        }
      });
    }
  }

  deleteStaff(id: string): void {
    if (!confirm('Bu personeli silmek istediğinizden emin misiniz?')) return;

    this.staffService.delete(id).subscribe({
      next: () => this.loadStaff()
    });
  }

  getInitials(name: string): string {
    return name.split(' ').map(n => n[0]).join('').toUpperCase().slice(0, 2);
  }
}