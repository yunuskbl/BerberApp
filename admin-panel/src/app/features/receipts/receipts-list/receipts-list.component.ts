import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ReceiptService } from '../../../core/services/receipt.service';
import { Receipt, CreateReceiptItemDto } from '../../../core/models/receipt.model';

@Component({
  selector: 'app-receipts-list',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './receipts-list.component.html',
  styleUrl: './receipts-list.component.scss',
})
export class ReceiptsListComponent implements OnInit {
  receipts: Receipt[] = [];
  isLoading = true;

  filterFrom = '';
  filterTo   = '';

  // Manual receipt creation
  isCreateOpen   = false;
  isSubmitting   = false;
  createError    = '';
  newItems: { serviceName: string; quantity: number; unitPrice: number }[] = [];
  newNote = '';

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

  openCreate(): void {
    this.isCreateOpen = true;
    this.newItems     = [{ serviceName: '', quantity: 1, unitPrice: 0 }];
    this.newNote      = '';
    this.createError  = '';
  }

  closeCreate(): void { this.isCreateOpen = false; }

  addItem(): void { this.newItems.push({ serviceName: '', quantity: 1, unitPrice: 0 }); }
  removeItem(i: number): void { this.newItems.splice(i, 1); }

  get newTotal(): number {
    return this.newItems.reduce((s, i) => s + i.quantity * i.unitPrice, 0);
  }

  submitCreate(): void {
    if (this.newItems.some(i => !i.serviceName || i.unitPrice <= 0)) {
      this.createError = 'Hizmet adı ve fiyat zorunludur.';
      return;
    }
    this.isSubmitting = true;
    this.createError  = '';
    this.receiptService.create({
      note:  this.newNote || undefined,
      items: this.newItems.map(i => ({ serviceName: i.serviceName, quantity: i.quantity, unitPrice: i.unitPrice })),
    }).subscribe({
      next: (res) => {
        this.isSubmitting = false;
        if (res.success) { this.receipts.unshift(res.data); this.closeCreate(); }
        else this.createError = 'Hata oluştu.';
      },
      error: (err) => { this.createError = err.error?.message || 'Hata oluştu.'; this.isSubmitting = false; },
    });
  }

  voidReceipt(id: string): void {
    if (!confirm('Bu fişi iptal etmek istediğinizden emin misiniz?')) return;
    this.receiptService.void(id).subscribe({
      next: () => {
        const r = this.receipts.find(x => x.id === id);
        if (r) r.isVoid = true;
      },
    });
  }

  printReceipt(receipt: Receipt): void {
    const w = window.open('', '_blank', 'width=400,height=600');
    if (!w) return;
    w.document.write(this.buildPrintHtml(receipt));
    w.document.close();
    w.focus();
    setTimeout(() => { w.print(); w.close(); }, 400);
  }

  buildPrintHtml(r: Receipt): string {
    const itemRows = r.items.map(i =>
      `<tr><td>${i.serviceName}</td><td style="text-align:right">${i.quantity}x</td><td style="text-align:right">${(i.unitPrice).toFixed(2)}₺</td></tr>`
    ).join('');
    const date = new Date(r.createdAt).toLocaleDateString('tr-TR');
    return `<!DOCTYPE html><html><head><meta charset="UTF-8"><style>
      body{font-family:monospace;font-size:13px;margin:0;padding:16px;width:280px}
      h2{text-align:center;font-size:15px;margin:0 0 4px}
      .sub{text-align:center;color:#666;margin-bottom:8px}
      hr{border:none;border-top:1px dashed #999;margin:8px 0}
      table{width:100%;border-collapse:collapse}
      td{padding:2px 0}
      .total{font-weight:bold;font-size:15px}
      .footer{text-align:center;margin-top:10px;font-size:11px;color:#888}
    </style></head><body>
      <h2>FİŞ</h2>
      <div class="sub">No: ${r.receiptNumber}</div>
      <div class="sub">Tarih: ${date}</div>
      ${r.customerName ? `<div class="sub">Müşteri: ${r.customerName}</div>` : ''}
      <hr>
      <table>${itemRows}</table>
      <hr>
      <table><tr><td class="total">TOPLAM</td><td style="text-align:right" class="total">${r.totalAmount.toFixed(2)}₺</td></tr></table>
      <div class="footer">Teşekkür ederiz</div>
    </body></html>`;
  }

  formatDate(d: string): string {
    return new Date(d).toLocaleDateString('tr-TR', { day: '2-digit', month: 'short', year: 'numeric' });
  }
}
