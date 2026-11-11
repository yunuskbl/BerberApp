import { Receipt } from './models/receipt.model';
import { formatAmount } from './pricing';

/**
 * Fişi yazdırma penceresinde açar.
 *
 * Bu şablon müşteri listesinde ve Fişler sayfasında iki ayrı kopya olarak
 * duruyordu. İkisi de hizmet adını, müşteri adını ve notu HTML'e kaçış
 * uygulamadan basıyordu: adında "<" geçen bir hizmet yazdırılan sayfayı
 * bozuyor, betik içeren bir ad o pencerede çalışabiliyordu. Satırlarda yalnız
 * birim fiyat vardı ("2x 250₺"), satır tutarını okuyan hesaplamak
 * zorundaydı; artık adet × birim fiyat ve tutar ayrı yazılıyor.
 */
export function printReceipt(receipt: Receipt): void {
  const w = window.open('', '_blank', 'width=400,height=600');
  if (!w) return;
  w.document.write(buildReceiptHtml(receipt));
  w.document.close();
  w.focus();
  setTimeout(() => { w.print(); w.close(); }, 400);
}

export function buildReceiptHtml(r: Receipt): string {
  const money = (v: number) => `₺${formatAmount(v)}`;

  const rows = r.items.map(i => `
    <tr>
      <td>${escapeHtml(i.serviceName)}</td>
      <td class="num">${i.quantity} × ${money(i.unitPrice)}</td>
      <td class="num">${money(i.quantity * i.unitPrice)}</td>
    </tr>`).join('');

  const date = new Date(r.createdAt).toLocaleDateString('tr-TR', {
    day: '2-digit', month: '2-digit', year: 'numeric',
    hour: '2-digit', minute: '2-digit', timeZone: 'Europe/Istanbul',
  });

  return `<!DOCTYPE html><html lang="tr"><head><meta charset="UTF-8"><title>Fiş ${escapeHtml(r.receiptNumber)}</title><style>
    body{font-family:monospace;font-size:13px;margin:0;padding:16px;width:280px}
    h2{text-align:center;font-size:15px;margin:0 0 4px}
    .sub{text-align:center;color:#666;margin-bottom:4px}
    hr{border:none;border-top:1px dashed #999;margin:8px 0}
    table{width:100%;border-collapse:collapse}
    td{padding:2px 0;vertical-align:top}
    .num{text-align:right;white-space:nowrap;padding-left:6px}
    .total{font-weight:bold;font-size:15px}
    .note{margin-top:6px;font-size:12px}
    .void{text-align:center;font-weight:bold;color:#b91c1c;margin:6px 0}
    .footer{text-align:center;margin-top:10px;font-size:11px;color:#888}
  </style></head><body>
    <h2>FİŞ</h2>
    <div class="sub">No: ${escapeHtml(r.receiptNumber)}</div>
    <div class="sub">Tarih: ${date}</div>
    ${r.customerName ? `<div class="sub">Müşteri: ${escapeHtml(r.customerName)}</div>` : ''}
    ${r.isVoid ? '<div class="void">İPTAL EDİLDİ</div>' : ''}
    <hr><table>${rows}</table><hr>
    <table><tr><td class="total">TOPLAM</td><td class="num total">${money(r.totalAmount)}</td></tr></table>
    ${r.note ? `<div class="note">Not: ${escapeHtml(r.note)}</div>` : ''}
    <div class="footer">Teşekkür ederiz</div>
  </body></html>`;
}

function escapeHtml(value: string | null | undefined): string {
  return (value ?? '')
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
    .replace(/'/g, '&#39;');
}
