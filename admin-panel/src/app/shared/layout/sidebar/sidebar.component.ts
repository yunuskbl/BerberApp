import { Component, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { Router } from '@angular/router';

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './sidebar.component.html',
  styleUrl: './sidebar.component.scss'
})
export class SidebarComponent {
  isCollapsed = false;

  menuItems = [
    { label: 'Dashboard',  icon: '▦',  route: '/dashboard'    },
    { label: 'Randevular', icon: '📅', route: '/appointments' },
    { label: 'Personel',   icon: '👤', route: '/staff'        },
    { label: 'Hizmetler',  icon: '✂',  route: '/services'    },
    { label: 'Müşteriler', icon: '👥', route: '/customers'   },
  ];

  bottomItems = [
    { label: 'Ayarlar', icon: '⚙', route: '/settings' },
  ];

  constructor(
    private authService: AuthService,
    private router: Router
  ) {
    this.checkScreenSize();
  }

  @HostListener('window:resize')
  checkScreenSize(): void {
    this.isCollapsed = window.innerWidth < 768;
  }

  get user() { return this.authService.getUser(); }

  toggle(): void {
    this.isCollapsed = !this.isCollapsed;
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }

  navigateTo(route: string): void {
    this.router.navigate([route]);
    if (window.innerWidth < 768) {
      this.isCollapsed = true;
    }
  }
}