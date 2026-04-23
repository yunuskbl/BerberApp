import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { MainLayoutComponent } from './shared/layout/main-layout/main-layout.component';
import { ReportsListComponent } from './features/reports/reports-list/reports-list.component';
import { PlanGuard } from './core/guards/plan.guard';

export const routes: Routes = [
  // Public routes — guard yok
  {
    path: 'login',
    loadComponent: () =>
      import('./features/auth/login/login.component').then(
        (m) => m.LoginComponent,
      ),
  },
  {
    path: 'salons',
    loadComponent: () =>
      import('./features/salons/salons.component').then(
        (m) => m.SalonsComponent,
      ),
  },
  {
    path: 'book/:subdomain',
    loadComponent: () =>
      import('./features/booking/booking.component').then(
        (m) => m.BookingComponent,
      ),
  },
  {
    path: 'randevu/:subdomain/:appointmentId',
    loadComponent: () =>
      import('./features/booking/appointment-status/appointment-status.component').then(
        (m) => m.AppointmentStatusComponent,
      ),
  },
  {
        path: 'pricing', 
        loadComponent: () =>
          import('./features/pricing/pricing.component').then(
            (m) => m.PricingComponent,
          ),
      },

  // Protected routes — guard var
  {
    path: '',
    component: MainLayoutComponent,
    canActivate: [authGuard],
    children: [
      {
        path: 'dashboard',
        loadComponent: () =>
          import('./features/dashboard/dashboard.component').then(
            (m) => m.DashboardComponent,
          ),
      },
      {
        path: 'upgrade',
        loadComponent: () =>
          import('./features/upgrade/upgrade.component').then(
            (m) => m.UpgradeComponent,
          ),
      },

      {
        path: 'staff',
        loadComponent: () =>
          import('./features/staff/staff-list/staff-list.component').then(
            (m) => m.StaffListComponent,
          ),
      },
      {
        path: 'reports',
        loadComponent: () =>
          import('./features/reports/reports-list/reports-list.component').then(
            (m) => m.ReportsListComponent,
          ),
        canActivate: [PlanGuard],
        data: { requiredPlan: 'Standard' },
      },
      {
        path: 'settings',
        loadComponent: () =>
          import('./features/settings/settings.component').then(
            (m) => m.SettingsComponent,
          ),
      },
      {
        path: 'services',
        loadComponent: () =>
          import('./features/services/service-list/service-list.component').then(
            (m) => m.ServiceListComponent,
          ),
      },
      {
        path: 'customers',
        loadComponent: () =>
          import('./features/customers/customer-list/customer-list.component').then(
            (m) => m.CustomerListComponent,
          ),
      },
      {
        path: 'appointments',
        loadComponent: () =>
          import('./features/appointments/appointment-list/appointment-list.component').then(
            (m) => m.AppointmentListComponent,
          ),
      },
      {
        path: '',
        redirectTo: 'dashboard',
        pathMatch: 'full',
      },
    ],
  },
  {
    path: '**',
    redirectTo: 'login',
  },
];
