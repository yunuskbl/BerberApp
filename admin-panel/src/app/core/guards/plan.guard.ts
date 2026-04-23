import { Injectable } from '@angular/core';
import { CanActivate, ActivatedRouteSnapshot, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

@Injectable({
  providedIn: 'root',
})
export class PlanGuard implements CanActivate {
  private planLevels: { [key: string]: number } = {
    Basic: 1,
    Standard: 2,
    Full: 3,
  };

  constructor(
    private authService: AuthService,
    private router: Router,
  ) {}

  // plan.guard.ts
  canActivate(route: ActivatedRouteSnapshot): boolean {
    const requiredPlan = route.data['requiredPlan'] as string;
    const userPlan = this.authService.getUserPlan();

    const userLevel = this.planLevels[userPlan] || 1;
    const requiredLevel = this.planLevels[requiredPlan] || 1;

    if (userLevel >= requiredLevel) {
      return true;
    }

    // Login yerine /upgrade'e yönlendir!
    this.router.navigate(['/upgrade'], {
      queryParams: { current: userPlan, required: requiredPlan },
    });
    return false;
  }
}
