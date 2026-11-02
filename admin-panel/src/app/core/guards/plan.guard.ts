import { Injectable } from '@angular/core';
import { CanActivate, ActivatedRouteSnapshot, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { PlanName, planAtLeast } from '../plans';

@Injectable({ providedIn: 'root' })
export class PlanGuard implements CanActivate {

  constructor(private authService: AuthService, private router: Router) {}

  canActivate(route: ActivatedRouteSnapshot): boolean {
    // Plan hiyerarşisi core/plans.ts'te; kenar çubuğundaki kilit de aynı
    // yerden okuyor ki menü ile rota birbirinden sapmasın.
    const requiredPlan = route.data['requiredPlan'] as PlanName;
    const userPlan     = this.authService.getUserPlan();

    if (planAtLeast(userPlan, requiredPlan)) return true;

    this.router.navigate(['/upgrade'], {
      queryParams: { current: userPlan, required: requiredPlan },
    });
    return false;
  }
}
