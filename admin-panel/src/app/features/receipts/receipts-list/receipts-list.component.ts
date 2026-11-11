import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ReceiptService } from '../../../core/services/receipt.service';
import { Receipt } from '../../../core/models/receipt.model';
import { ReceiptFormComponent } from '../../../shared/components/receipt-form/receipt-form.component';
import { printReceipt } from '../../../core/receipt-print';
import { formatAmount } from '../../../core/pricing';

@Component({
  selector: 'app-receipts-list',
  standalone: true,
  imports: [CommonModule, FormsModule, ReceiptFormComponent],
  templateUrl: './receipts-list.component.html',
  styleUrl: './receipts-list.component.scss',
})
export class ReceiptsListComponent implements OnInit {
  receipts: Receipt[] = [];
  isLoading = true;

  filterFrom = '';
  filterTo   = '';

  // Elle fiş kesme penceresi — form ReceiptFormComponent'te.
  isCreateOpen = false;
  voidingId: string | null = null;

  constructor(private receiptService: ReceiptService) {}

  ngOnInit(): void { this.load(); }

  load(): void {
    this.isLoading = true;
    this.receiptService.getAll({
      from: this.filterFrom || undefined,
      to:   this.filterTo   || undefined,
    }).subscribe({
      next: (res) => { if (res.success) this.receipts = res.data; this.isLoading = false; },
      error: () => { this.isLoading = false; },
    });
  }

  openCreate(): void  { this.isCreateOpen = true; }
  closeCreate(): void { this.isCreateOpen = false; }

  onCreated(receipt: Receipt): void {
    this.receipts = [receipt, ...this.receipts];
    this.isCreateOpen = false;
  }

  voidReceipt(id: string): void {
    if (this.voidingId) return;
    if (!confirm('Bu fişi iptal etmek istediğinizden emin misiniz?')) return;
    this.voidingId = id;
    this.receiptService.void(id).subscribe({
      next: () => {
        this.voidingId = null;
        this.receipts = this.receipts.map(r => r.id === id ? { ...r, isVoid: true } : r);
      },
      error: (err) => {
        this.voidingId = null;
        alert(err?.error?.message || 'Fiş iptal edilemedi. Lütfen tekrar deneyin.');
      },
    });
  }

  print(receipt: Receipt): void { printReceipt(receipt); }

  money(value: number): string { return `₺${formatAmount(value)}`; }

  formatDate(d: string): string {
    return new Date(d).toLocaleDateString('tr-TR', { day: '2-digit', month: 'short', year: 'numeric' });
  }
}
