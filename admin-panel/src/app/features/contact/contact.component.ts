import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';

interface PaymentMethodPublic {
  id: string;
  name: string;
  bankName: string;
  iban: string;
  accountHolder: string;
  description?: string;
}

interface MyMessage {
  id: string;
  subject: string;
  message: string;
  status: 'New' | 'Read' | 'Replied';
  reply?: string;
  repliedAt?: string;
  isReplySeen: boolean;
  createdAt: string;
}

@Component({
  selector: 'app-contact',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './contact.component.html',
  styleUrl: './contact.component.scss'
})
export class ContactComponent implements OnInit {
  activeTab: 'send' | 'messages' | 'payment' = 'send';

  paymentMethods: PaymentMethodPublic[] = [];
  isLoadingMethods = true;

  myMessages: MyMessage[] = [];
  isLoadingMessages = false;
  expandedId: string | null = null;

  subject = '';
  message = '';
  isSending = false;
  successMessage = '';
  errorMessage = '';

  constructor(private http: HttpClient) {}

  ngOnInit(): void {
    this.loadPaymentMethods();
    this.loadMessages();
  }

  loadPaymentMethods(): void {
    this.http.get<any>(`${environment.apiUrl}/contact/payment-methods`).subscribe({
      next: res => { if (res.success) this.paymentMethods = res.data; this.isLoadingMethods = false; },
      error: () => { this.isLoadingMethods = false; }
    });
  }

  loadMessages(): void {
    this.isLoadingMessages = true;
    this.http.get<any>(`${environment.apiUrl}/contact/messages`).subscribe({
      next: res => { if (res.success) this.myMessages = res.data; this.isLoadingMessages = false; },
      error: () => { this.isLoadingMessages = false; }
    });
  }

  get unreadReplies(): number {
    return this.myMessages.filter(m => m.status === 'Replied' && !m.isReplySeen).length;
  }

  setTab(tab: 'send' | 'messages' | 'payment'): void { this.activeTab = tab; }

  toggle(id: string): void {
    this.expandedId = this.expandedId === id ? null : id;
    if (this.expandedId === id) {
      const msg = this.myMessages.find(m => m.id === id);
      if (msg && msg.reply && !msg.isReplySeen) {
        msg.isReplySeen = true;
        this.http.patch(`${environment.apiUrl}/contact/messages/${id}/seen`, {}).subscribe();
      }
    }
  }

  statusLabel(s: string): string {
    return { New: 'Bekliyor', Read: 'İnceleniyor', Replied: 'Yanıtlandı' }[s] ?? s;
  }

  formatDate(d: string): string {
    return new Date(d).toLocaleDateString('tr-TR', {
      day: '2-digit', month: 'short', year: 'numeric',
      hour: '2-digit', minute: '2-digit'
    });
  }

  send(): void {
    if (!this.subject.trim() || !this.message.trim()) {
      this.errorMessage = 'Konu ve mesaj alanları zorunludur.';
      return;
    }
    this.isSending = true;
    this.errorMessage = '';
    this.http.post<any>(`${environment.apiUrl}/contact/messages`, {
      subject: this.subject,
      message: this.message
    }).subscribe({
      next: res => {
        if (res.success) {
          this.successMessage = 'Mesajınız iletildi. En kısa sürede yanıtlanacaktır.';
          this.subject = '';
          this.message = '';
          this.loadMessages();
          setTimeout(() => this.setTab('messages'), 800);
        }
        this.isSending = false;
      },
      error: () => {
        this.errorMessage = 'Mesaj gönderilemedi. Lütfen tekrar deneyin.';
        this.isSending = false;
      }
    });
  }
}
