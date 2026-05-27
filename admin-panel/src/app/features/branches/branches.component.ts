import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { BranchService, Branch, CreateBranchRequest } from '../../core/services/branch.service';

@Component({
  selector: 'app-branches',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './branches.component.html',
  styleUrl: './branches.component.scss',
})
export class BranchesComponent implements OnInit {
  branches: Branch[] = [];
  isLoading = true;
  errorMessage = '';

  isModalOpen = false;
  isSubmitting = false;
  submitError = '';

  form: CreateBranchRequest = { name: '', subdomain: '', phone: '', address: '', city: '' };

  deleteConfirmId: string | null = null;
  isDeleting = false;

  constructor(private branchService: BranchService) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.isLoading = true;
    this.branchService.getAll().subscribe({
      next: (res) => {
        this.branches = res.data ?? res ?? [];
        this.isLoading = false;
      },
      error: (err) => {
        this.errorMessage = err?.error?.message ?? 'Şubeler yüklenemedi.';
        this.isLoading = false;
      },
    });
  }

  openModal(): void {
    this.form = { name: '', subdomain: '', phone: '', address: '', city: '' };
    this.submitError = '';
    this.isModalOpen = true;
  }

  closeModal(): void {
    this.isModalOpen = false;
  }

  onSubdomainInput(): void {
    this.form.subdomain = this.form.subdomain
      .toLowerCase()
      .replace(/[^a-z0-9-]/g, '-')
      .replace(/-+/g, '-');
  }

  onSubmit(): void {
    if (!this.form.name.trim() || !this.form.subdomain.trim()) return;
    this.isSubmitting = true;
    this.submitError = '';

    this.branchService.create(this.form).subscribe({
      next: () => {
        this.isSubmitting = false;
        this.isModalOpen = false;
        this.load();
      },
      error: (err) => {
        this.submitError = err?.error?.message ?? 'Şube oluşturulamadı.';
        this.isSubmitting = false;
      },
    });
  }

  confirmDelete(id: string): void {
    this.deleteConfirmId = id;
  }

  cancelDelete(): void {
    this.deleteConfirmId = null;
  }

  deleteBranch(id: string): void {
    this.isDeleting = true;
    this.branchService.delete(id).subscribe({
      next: () => {
        this.deleteConfirmId = null;
        this.isDeleting = false;
        this.branches = this.branches.filter(b => b.id !== id);
      },
      error: () => {
        this.isDeleting = false;
        this.deleteConfirmId = null;
      },
    });
  }

  bookingUrl(subdomain: string): string {
    return `/book/${subdomain}`;
  }

  formatLocation(address?: string, city?: string): string {
    return [address, city].filter(v => v).join(', ');
  }
}
