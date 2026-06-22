import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-super-admin-sidebar',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './super-admin-sidebar.component.html',
  styleUrl: './super-admin-sidebar.component.scss'
})
export class SuperAdminSidebarComponent {
  menuItems = [
    { icon: '🏢', label: 'İşletmeler',      route: '/superadmin/tenants' },
    { icon: '📊', label: 'Raporlar',         route: '/superadmin/reports' },
    { icon: '💳', label: 'Ödemeler',         route: '/superadmin/payments' },
    { icon: '🏦', label: 'Ödeme Yöntemleri', route: '/superadmin/payment-methods' },
    { icon: '✉️', label: 'Mesajlar',          route: '/superadmin/messages' },
    { icon: '🛡️', label: 'Güvenlik Logları', route: '/superadmin/audit-logs' },
    { icon: '💬', label: 'WhatsApp',         route: '/superadmin/whatsapp' },
  ];

  constructor(private authService: AuthService, private router: Router) {}

  get user() { return this.authService.getUser(); }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }
}
