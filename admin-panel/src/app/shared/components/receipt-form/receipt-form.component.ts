import {
  Component, ElementRef, OnInit, QueryList, ViewChildren, input, output, signal,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ReceiptService } from '../../../core/services/receipt.service';
import { ServiceService } from '../../../core/services/service.service';
import { Receipt, CreateReceiptItemDto } from '../../../core/models/receipt.model';
import { Service } from '../../../core/models/service.model';
import { formatAmount } from '../../../core/pricing';

/** Forma ön doldurulan kalem (ör. randevudaki hizmet). */
export interface ReceiptLineSeed {
  serviceName: string;
  quantity?: number;
  unitPrice?: number | null;
}

interface ReceiptRow {
  key: number;
  serviceName: string;
  quantity: number | null;
  unitPrice: number | null;
}

type RowProblem = 'name' | 'quantity' | 'price';

// Sunucudaki CreateReceiptCommandValidator ile aynı sınırlar.
const MAX_ITEMS      = 50;
const MAX_QUANTITY   = 999;
const MAX_UNIT_PRICE = 1_000_000;

/**
 * Fiş kesme formu: kalem satırları, toplam, not ve gönderim.
 *
 * Aynı form müşteri geçmişindeki "Fiş Kes" penceresinde ve Fişler sayfasındaki
 * "Yeni Fiş" penceresinde iki ayrı kopya olarak duruyordu ve ikisinde de aynı
 * kusurlar vardı:
 *  - Girişlerin sınıfı (`form-input`) hiçbir yerde tanımlı değildi; tarayıcının
 *    varsayılan genişlikteki kutuları satıra sığmıyor, silme düğmesi pencerenin
 *    dışına itiliyordu. Tek satır kaldığında düğme hiç çizilmiyordu.
 *  - "+ Ekle" ile açılıp boş bırakılan satır silinemediği için fiş
 *    "Hizmet adı ve fiyat zorunludur." hatasıyla kesilemiyordu.
 *  - Satır tutarı yoktu, toplam "250.00 ₺" gibi İngilizce biçimde yazılıyordu.
 *
 * Artık her satırın silme düğmesi her zaman görünür, tamamen boş satırlar
 * gönderimde yok sayılır, hatalı satır işaretlenir ve sunucunun doğrulama
 * mesajları olduğu gibi gösterilir.
 */
@Component({
  selector: 'app-receipt-form',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './receipt-form.component.html',
  styleUrl: './receipt-form.component.scss',
})
export class ReceiptFormComponent implements OnInit {
  readonly customerId    = input<string | undefined>(undefined);
  readonly appointmentId = input<string | undefined>(undefined);
  readonly initialItems  = input<ReceiptLineSeed[]>([]);

  readonly created   = output<Receipt>();
  readonly cancelled = output<void>();

  @ViewChildren('nameInput') private nameInputs!: QueryList<ElementRef<HTMLInputElement>>;

  readonly maxItems    = MAX_ITEMS;
  readonly maxQuantity = MAX_QUANTITY;
  readonly maxPrice    = MAX_UNIT_PRICE;
  /** Aynı sayfada iki form olursa datalist kimlikleri çakışmasın. */
  readonly listId = `receipt-services-${Math.random().toString(36).slice(2, 8)}`;

  rows: ReceiptRow[] = [];
  note = '';

  readonly services   = signal<Service[]>([]);
  readonly submitting = signal(false);
  /** İlk gönderim denemesinden sonra hatalı satırlar kırmızı gösterilir. */
  readonly attempted  = signal(false);
  /** Sunucudan dönen hata; bir sonraki gönderimde temizlenir. */
  readonly serverError = signal('');

  private nextKey = 1;

  constructor(
    private receiptService: ReceiptService,
    private serviceService: ServiceService,
  ) {}

  ngOnInit(): void {
    const seeds = this.initialItems();
    this.rows = seeds.length
      ? seeds.map(s => this.makeRow(s.serviceName, s.quantity ?? 1, s.unitPrice ?? null))
      : [this.makeRow()];

    // Öneri listesi yalnızca yardımcı; gelmezse form yine çalışır.
    this.serviceService.getAll().subscribe({
      next: res => {
        if (!res.success) return;
        this.services.set((res.data ?? []).filter(s => s.isActive));
        // Liste gelmeden yazılmış hizmetlerin fiyatını da getir.
        this.rows.forEach(r => this.fillPriceFromService(r));
      },
      error: () => {},
    });
  }

  // ─── Satırlar ────────────────────────────────────────────

  get canAddRow(): boolean { return this.rows.length < MAX_ITEMS; }

  addRow(): void {
    if (!this.canAddRow) return;
    this.rows.push(this.makeRow());
    // Yeni satır çizildikten sonra imleci hizmet adına taşı.
    setTimeout(() => this.nameInputs?.last?.nativeElement.focus());
  }

  /**
   * Satırı siler. Son kalan satır silinmez, boşaltılır: pencerede her zaman
   * yazılabilecek bir satır kalsın.
   */
  removeRow(index: number): void {
    if (this.rows.length > 1) {
      this.rows.splice(index, 1);
    } else {
      this.rows = [this.makeRow()];
    }
  }

  /** Tanımlı bir hizmet seçildiyse ve fiyat henüz girilmediyse fiyatı getirir. */
  fillPriceFromService(row: ReceiptRow): void {
    if (row.unitPrice != null && row.unitPrice > 0) return;
    const match = this.findService(row.serviceName);
    if (match && match.price > 0 && (match.currency ?? 'TRY') === 'TRY') {
      row.unitPrice = match.price;
    }
  }

  /** Alandan çıkınca adı tanımlı hizmetin yazımına çevirir ("saç kesimi" → "Saç Kesimi"). */
  normalizeName(row: ReceiptRow): void {
    const match = this.findService(row.serviceName);
    row.serviceName = match ? match.name : row.serviceName.trim();
  }

  // ─── Hesap ───────────────────────────────────────────────

  isBlank(row: ReceiptRow): boolean {
    return !row.serviceName.trim() && (row.unitPrice == null || row.unitPrice === 0);
  }

  problemsOf(row: ReceiptRow): RowProblem[] {
    if (this.isBlank(row)) return [];
    const problems: RowProblem[] = [];
    if (!row.serviceName.trim()) problems.push('name');
    const q = row.quantity;
    if (q == null || !Number.isInteger(q) || q < 1 || q > MAX_QUANTITY) problems.push('quantity');
    const p = row.unitPrice;
    if (p == null || !(p > 0) || p > MAX_UNIT_PRICE) problems.push('price');
    return problems;
  }

  showProblem(row: ReceiptRow, problem: RowProblem): boolean {
    return this.attempted() && this.problemsOf(row).includes(problem);
  }

  lineTotal(row: ReceiptRow): number | null {
    const q = row.quantity, p = row.unitPrice;
    if (q == null || p == null || !(q > 0) || !(p > 0)) return null;
    return Math.round(q * p * 100) / 100;
  }

  get total(): number {
    const sum = this.rows.reduce((s, r) => s + (this.lineTotal(r) ?? 0), 0);
    return Math.round(sum * 100) / 100;
  }

  money(value: number): string { return `₺${formatAmount(value)}`; }

  /**
   * Gösterilecek hata. Form hataları satırlar düzeldikçe kendiliğinden kalkar;
   * sunucu hatası ise bir sonraki denemeye kadar durur.
   */
  get errorMessage(): string {
    if (this.attempted()) {
      const filled = this.rows.filter(r => !this.isBlank(r));
      if (filled.length === 0) return 'Fişe en az bir hizmet ekleyin.';
      if (filled.some(r => this.problemsOf(r).length > 0)) {
        return 'İşaretli satırları düzeltin: hizmet adı, 1–999 arası adet ve sıfırdan büyük fiyat gerekli.';
      }
    }
    return this.serverError();
  }

  // ─── Gönderim ────────────────────────────────────────────

  submit(): void {
    if (this.submitting()) return;
    this.attempted.set(true);
    this.serverError.set('');
    if (this.errorMessage) return;

    const filled = this.rows.filter(r => !this.isBlank(r));

    const items: CreateReceiptItemDto[] = filled.map(r => ({
      serviceName: r.serviceName.trim(),
      quantity:    r.quantity!,
      unitPrice:   Math.round(r.unitPrice! * 100) / 100,
    }));

    this.submitting.set(true);
    this.receiptService.create({
      customerId:    this.customerId(),
      appointmentId: this.appointmentId(),
      note:          this.note.trim() || undefined,
      items,
    }).subscribe({
      next: res => {
        this.submitting.set(false);
        if (res.success && res.data) this.created.emit(res.data);
        else this.serverError.set(res.message || 'Fiş kesilemedi.');
      },
      error: err => {
        this.submitting.set(false);
        const body = err?.error;
        const details: string[] = Array.isArray(body?.errors) ? body.errors : [];
        this.serverError.set(
          details.length ? details.join(' ')
          : body?.message || (err?.status === 0
              ? 'Sunucuya ulaşılamadı. Bağlantınızı kontrol edip tekrar deneyin.'
              : 'Fiş kesilemedi. Lütfen tekrar deneyin.'),
        );
      },
    });
  }

  cancel(): void { this.cancelled.emit(); }

  // ─── Yardımcılar ─────────────────────────────────────────

  private makeRow(serviceName = '', quantity: number | null = 1, unitPrice: number | null = null): ReceiptRow {
    return { key: this.nextKey++, serviceName, quantity, unitPrice: unitPrice && unitPrice > 0 ? unitPrice : null };
  }

  private findService(name: string): Service | undefined {
    const wanted = name.trim().toLocaleLowerCase('tr-TR');
    if (!wanted) return undefined;
    return this.services().find(s => s.name.trim().toLocaleLowerCase('tr-TR') === wanted);
  }
}
