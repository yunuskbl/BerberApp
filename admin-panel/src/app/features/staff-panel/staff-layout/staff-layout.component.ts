import { Component } from '@angular/core';
import { Router, RouterModule, RouterLinkActive } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-staff-layout',
  standalone: true,
  imports: [CommonModule, RouterModule, RouterLinkActive],
  templateUrl: './staff-layout.component.html',
  styleUrl: './staff-layout.component.scss',
})
export class StaffLayoutComponent {
  sidebarOpen = false;

  constructor(public authService: AuthService, private router: Router) {}

  get staffName(): string {
    return this.authService.getUser()?.fullName || 'Personel';
  }

  get staffEmail(): string {
    return this.authService.getUser()?.email || '';
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }
}
