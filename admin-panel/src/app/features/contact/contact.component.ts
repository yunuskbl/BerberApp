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

@Component({
  selector: 'app-contact',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './contact.component.html',
  styleUrl: './contact.component.scss'
})
export class ContactComponent implements OnInit {
  paymentMethods: PaymentMethodPublic[] = [];
  isLoadingMethods = true;

  subject = '';
  message = '';
  isSending = false;
  successMessage = '';
  errorMessage = '';

  constructor(private http: HttpClient) {}

  ngOnInit(): void {
    this.http.get<any>(`${environment.apiUrl}/contact/payment-methods`).subscribe({
      next: res => { if (res.success) this.paymentMethods = res.data; this.isLoadingMethods = false; },
      error: () => { this.isLoadingMethods = false; }
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
