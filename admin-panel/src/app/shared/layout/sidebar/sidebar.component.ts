import { Component, EventEmitter, HostListener, Output } from '@angular/core';
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
  @Output() collapsedChange = new EventEmitter<boolean>();  
  menuItems = [
    { label: 'Dashboard',  icon: '▦',  route: '/dashboard'    },
    { label: 'Randevular', icon: '📅', route: '/appointments' },
    { label: 'Personel',   icon: '👤', route: '/staff'        },
    { label: 'Hizmetler',  icon: '✂',  route: '/services'    },
    { label: 'Müşteriler', icon: '👥', route: '/customers'  },
    { label: 'Raporlar',   icon: '📊', route: '/reports'    },
  ];

  bottomItems = [
    { label: 'Ayarlar', icon: '⚙', route: '/settings' },
  ];

  constructor(
    private authService: AuthService,
    private router: Router
  ) { }

  @HostListener('window:resize')


  get user() { return this.authService.getUser(); }

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