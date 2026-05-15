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