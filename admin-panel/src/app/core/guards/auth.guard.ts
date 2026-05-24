import { Injectable } from '@angular/core';
import { ActivatedRouteSnapshot, CanActivate, Router, RouterStateSnapshot } from '@angular/router';
import { AuthService } from '../services/auth.service';

@Injectable({
  providedIn: 'root'
})
export class authGuard implements CanActivate {
  constructor(
    private authService: AuthService,
    private router: Router
  ) {}

  canActivate(_route: ActivatedRouteSnapshot, state: RouterStateSnapshot): boolean {
    if (!this.authService.isLoggedIn()) {
      this.router.navigate(['/login']);
      return false;
    }

    // Staff rolü admin paneline erişemez — staff paneline yönlendir
    if (this.authService.isStaff() && !state.url.startsWith('/staff-panel')) {
      this.router.navigate(['/staff-panel']);
      return false;
    }

    // /upgrade rotasını sonsuz döngüye sokmamak için geç
    if (state.url.startsWith('/upgrade')) {
      return true;
    }

    if (this.authService.isSubscriptionExpired() && !this.authService.isOnTrial()) {
      this.router.navigate(['/upgrade']);
      return false;
    }

    return true;
  }
}