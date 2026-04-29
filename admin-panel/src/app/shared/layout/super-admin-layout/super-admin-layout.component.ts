import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { Router } from '@angular/router';
import { SuperAdminSidebarComponent } from '../super-admin-sidebar/super-admin-sidebar.component';

@Component({
  selector: 'app-super-admin-layout',
  standalone: true,
  imports: [CommonModule, RouterModule, SuperAdminSidebarComponent],
  templateUrl: './super-admin-layout.component.html',
  styleUrl: './super-admin-layout.component.scss'
})
export class SuperAdminLayoutComponent {
  constructor(
    private authService: AuthService,
    private router: Router,
  ) {}

  get user() {
    return this.authService.getUser();
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }
}
