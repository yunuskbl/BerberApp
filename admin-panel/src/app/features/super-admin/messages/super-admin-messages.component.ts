import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { SuperAdminService, ContactMessage } from '../../../core/services/superadmin.service';

@Component({
  selector: 'app-super-admin-messages',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './super-admin-messages.component.html',
  styleUrl: './super-admin-messages.component.scss'
})
export class SuperAdminMessagesComponent implements OnInit {
  messages: ContactMessage[] = [];
  isLoading = true;
  errorMessage = '';
  successMessage = '';

  selectedStatus = '';
  selectedMessage: ContactMessage | null = null;
  replyText = '';
  isSendingReply = false;

  statusOptions = [
    { value: '', label: 'Tümü' },
    { value: 'New', label: 'Yeni' },
    { value: 'Read', label: 'Okundu' },
    { value: 'Replied', label: 'Yanıtlandı' },
  ];

  constructor(private svc: SuperAdminService) {}

  ngOnInit(): void { this.load(); }

  load(): void {
    this.isLoading = true;
    this.svc.getContactMessages(this.selectedStatus || undefined).subscribe({
      next: res => { if (res.success) this.messages = res.data; this.isLoading = false; },
      error: () => { this.errorMessage = 'Yüklenemedi.'; this.isLoading = false; }
    });
  }

  get newCount(): number { return this.messages.filter(m => m.status === 'New').length; }

  openMessage(msg: ContactMessage): void {
    this.selectedMessage = msg;
    this.replyText = msg.reply ?? '';
    if (msg.status === 'New') {
      this.svc.markMessageRead(msg.id).subscribe(() => {
        msg.status = 'Read';
      });
    }
  }

  closeMessage(): void { this.selectedMessage = null; this.replyText = ''; }

  sendReply(): void {
    if (!this.selectedMessage || !this.replyText.trim()) return;
    this.isSendingReply = true;
    this.svc.replyToMessage(this.selectedMessage.id, this.replyText).subscribe({
      next: res => {
        if (res.success) {
          this.selectedMessage!.reply = this.replyText;
          this.selectedMessage!.status = 'Replied';
          this.successMessage = 'Yanıt kaydedildi.';
          this.closeMessage();
          this.load();
        }
        this.isSendingReply = false;
      },
      error: () => { this.errorMessage = 'Yanıt gönderilemedi.'; this.isSendingReply = false; }
    });
  }

  deleteMsg(id: string, e: Event): void {
    e.stopPropagation();
    if (!confirm('Mesajı silmek istediğinize emin misiniz?')) return;
    this.svc.deleteContactMessage(id).subscribe({
      next: () => { this.successMessage = 'Silindi.'; this.load(); },
      error: () => { this.errorMessage = 'Silme başarısız.'; }
    });
  }

  statusLabel(s: string): string {
    return { New: 'Yeni', Read: 'Okundu', Replied: 'Yanıtlandı' }[s] ?? s;
  }

  formatDate(d: string): string {
    return new Date(d).toLocaleDateString('tr-TR', { day: '2-digit', month: 'short', year: 'numeric', hour: '2-digit', minute: '2-digit' });
  }
}
