import {
  Component, ElementRef, HostListener, OnInit, QueryList, ViewChildren, input, output, signal,
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
  /** Adet seçici açık mı. Kapalıyken satırda yalnızca "×1" görünür. */
  qtyOpen: boolean;
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
 *
 * Adet: berberde satırların neredeyse hepsi tek adet, ama ürün satışı ya da
 * tüp başı boya gibi durumlar var. Bu yüzden adet her satırda bir giriş kutusu
 * değil, sessiz bir "×1" düğmesi; dokununca − / + açılır. Hızlı ekle
 * düğmeleriyle aynı hizmet ikinci kez eklenirse yeni satır açılmaz, adet artar.
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
  /** Aynı sayfada iki form olursa öneri listesi kimlikleri çakışmasın. */
  readonly listId = `receipt-services-${Math.random().toString(36).slice(2, 8)}`;
  /** Öneri listesi en fazla bu kadar hizmet gösterir; gerisi yazarak süzülür. */
  readonly maxSuggestions = 8;

  rows: ReceiptRow[] = [];
  note = '';

  readonly services   = signal<Service[]>([]);
  readonly submitting = signal(false);
  /** İlk gönderim denemesinden sonra hatalı satırlar kırmızı gösterilir. */
  readonly attempted  = signal(false);
  /** Sunucudan dönen hata; bir sonraki gönderimde temizlenir. */
  readonly serverError = signal('');

  /** Öneri listesi açık olan satır ve klavyeyle seçili öneri. */
  suggestKey: number | null = null;
  activeIndex = -1;

  private nextKey = 1;

  constructor(
    private receiptService: ReceiptService,
    private serviceService: ServiceService,
    private host: ElementRef<HTMLElement>,
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

  /**
   * Hızlı ekle: aynı ad ve fiyatta satır varsa adedini artırır, yoksa ilk
   * boş satırı doldurur, o da yoksa yeni satır açar.
   */
  quickAdd(service: Service): void {
    const price = this.servicePrice(service);
    const same = this.rows.find(r =>
      this.sameName(r.serviceName, service.name) && (price == null || r.unitPrice === price));
    if (same) {
      same.quantity = this.clamp((same.quantity ?? 1) + 1);
      return;
    }
    const blank = this.rows.find(r => this.isBlank(r));
    if (blank) {
      blank.serviceName = service.name;
      blank.unitPrice   = price;
      blank.quantity    = 1;
      return;
    }
    if (this.canAddRow) this.rows.push(this.makeRow(service.name, 1, price));
  }

  // ─── Adet ────────────────────────────────────────────────

  openQty(row: ReceiptRow): void {
    row.qtyOpen = true;
    // Klavye kullanan için odağı "+"ya taşı; giriş kutusuna değil ki telefonda
    // klavye açılmasın.
    setTimeout(() =>
      this.host.nativeElement
        .querySelector<HTMLButtonElement>(`[data-qty-key="${row.key}"] .rf-step--up`)
        ?.focus());
  }

  stepQty(row: ReceiptRow, delta: number): void {
    row.quantity = this.clamp((row.quantity ?? 1) + delta);
  }

  /** Elle yazılan adedi 1–999 aralığına çeker; boş ya da geçersizse 1. */
  clampQty(row: ReceiptRow): void {
    row.quantity = this.clamp(row.quantity);
  }

  /**
   * Seçicinin dışına dokununca seçici kapanır ve satır yine "×N" görünür.
   * Odak kaybı yerine dokunuşa bakılıyor: iOS'ta düğmeye dokunmak odak
   * vermediği için odak olayıyla kapatmak "+" tıklamasını yutuyordu.
   */
  @HostListener('document:pointerdown', ['$event'])
  onDocumentPointerDown(event: PointerEvent): void {
    const target = event.target as Element | null;
    const holder = target?.closest?.('[data-qty-key]');
    const key = holder ? Number(holder.getAttribute('data-qty-key')) : null;
    for (const r of this.rows) {
      if (r.qtyOpen && r.key !== key) this.closeQty(r);
    }
  }

  /** Tanımlı bir hizmet seçildiyse ve fiyat henüz girilmediyse fiyatı getirir. */
  fillPriceFromService(row: ReceiptRow): void {
    if (row.unitPrice != null && row.unitPrice > 0) return;
    const match = this.findService(row.serviceName);
    const price = match ? this.servicePrice(match) : null;
    if (price != null) row.unitPrice = price;
  }

  // ─── Hizmet adı önerileri ────────────────────────────────
  //
  // Önceden tarayıcının kendi öneri kutusu (<datalist>) kullanılıyordu. O kutu
  // CSS ile biçimlenemiyor: Opera/Chrome'da koyu bir baloncuk olarak açılıyor,
  // kutunun içine de bir ▼ oku koyuyordu. Bu liste uygulamanın görünümünde,
  // fare, klavye (↑ ↓ Enter Esc) ve dokunmatikle çalışıyor.

  suggestionsFor(row: ReceiptRow): Service[] {
    const query = this.norm(row.serviceName);
    const all = this.services();
    const matches = query ? all.filter(s => this.norm(s.name).includes(query)) : all;
    // Yazılan ad zaten tek eşleşmeyle birebir aynıysa listeyi göstermenin anlamı yok.
    if (matches.length === 1 && this.norm(matches[0].name) === query) return [];
    return matches.slice(0, this.maxSuggestions);
  }

  isSuggestOpen(row: ReceiptRow): boolean {
    return this.suggestKey === row.key && this.suggestionsFor(row).length > 0;
  }

  openSuggest(row: ReceiptRow): void {
    this.suggestKey  = row.key;
    this.activeIndex = -1;
  }

  closeSuggest(): void {
    this.suggestKey  = null;
    this.activeIndex = -1;
  }

  onNameInput(row: ReceiptRow): void {
    this.fillPriceFromService(row);
    this.suggestKey = row.key;
    // Yazmaya başlayınca ilk öneri seçili gelsin: Enter onu alır.
    this.activeIndex = this.norm(row.serviceName) ? 0 : -1;
  }

  onNameKeydown(event: KeyboardEvent, row: ReceiptRow): void {
    const items = this.suggestionsFor(row);
    switch (event.key) {
      case 'ArrowDown':
        if (!items.length) return;
        event.preventDefault();
        this.suggestKey  = row.key;
        this.activeIndex = (this.activeIndex + 1) % items.length;
        break;
      case 'ArrowUp':
        if (!items.length) return;
        event.preventDefault();
        this.suggestKey  = row.key;
        this.activeIndex = this.activeIndex <= 0 ? items.length - 1 : this.activeIndex - 1;
        break;
      case 'Enter':
        if (this.isSuggestOpen(row) && this.activeIndex >= 0) {
          event.preventDefault();
          this.pick(row, items[this.activeIndex]);
        }
        break;
      case 'Escape':
        if (this.isSuggestOpen(row)) {
          event.preventDefault();
          event.stopPropagation();
          this.closeSuggest();
        }
        break;
    }
  }

  /** Öneriden seçilen hizmet: ad ve (TRY ise) fiyatı satıra yazılır. */
  pick(row: ReceiptRow, service: Service): void {
    row.serviceName = service.name;
    const price = this.servicePrice(service);
    if (price != null) row.unitPrice = price;
    this.closeSuggest();
  }

  onNameBlur(row: ReceiptRow): void {
    this.normalizeName(row);
    this.closeSuggest();
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
        return 'İşaretli satırları düzeltin: hizmet adı ve sıfırdan büyük fiyat gerekli.';
      }
    }
    return this.serverError();
  }

  // ─── Gönderim ────────────────────────────────────────────

  submit(): void {
    if (this.submitting()) return;
    this.rows.forEach(r => this.closeQty(r));
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
    return {
      key: this.nextKey++,
      serviceName,
      quantity: this.clamp(quantity),
      unitPrice: unitPrice && unitPrice > 0 ? unitPrice : null,
      qtyOpen: false,
    };
  }

  private closeQty(row: ReceiptRow): void {
    row.quantity = this.clamp(row.quantity);
    row.qtyOpen  = false;
  }

  private clamp(value: number | null | undefined): number {
    if (value == null || !Number.isFinite(value)) return 1;
    return Math.min(MAX_QUANTITY, Math.max(1, Math.round(value)));
  }

  /** Fiş TRY kesiliyor; başka para birimindeki hizmetin fiyatı getirilmez. */
  servicePrice(service: Service): number | null {
    return service.price > 0 && (service.currency ?? 'TRY') === 'TRY' ? service.price : null;
  }

  private norm(value: string): string {
    return value.trim().toLocaleLowerCase('tr-TR');
  }

  private sameName(a: string, b: string): boolean {
    return this.norm(a) === this.norm(b);
  }

  private findService(name: string): Service | undefined {
    if (!name.trim()) return undefined;
    return this.services().find(s => this.sameName(s.name, name));
  }
}
