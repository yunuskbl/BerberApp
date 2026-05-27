import { HttpInterceptorFn, HttpRequest, HttpHandlerFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, switchMap, throwError } from 'rxjs';
import { Router } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { BranchService } from '../services/branch.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const branchService = inject(BranchService);
  const router = inject(Router);

  // Only attach token to our own API; skip for external requests (geocoding etc.)
  if (!req.url.startsWith('/api')) {
    return next(req);
  }

  let cloned = addToken(req, authService.getToken());

  const activeBranch = branchService.activeBranch();
  if (activeBranch && !req.url.includes('/auth/')) {
    cloned = cloned.clone({ headers: cloned.headers.set('X-Branch-Id', activeBranch.id) });
  }

  return next(cloned).pipe(
    catchError((error: HttpErrorResponse) => {
      // Refresh endpoint'inin kendisi 401 dönerse sonsuz döngüye girme
      if (error.status === 401 && !req.url.includes('/auth/refresh') && !req.url.includes('/auth/login')) {
        const refreshToken = authService.getRefreshToken();

        if (refreshToken) {
          return authService.refreshToken().pipe(
            switchMap(() => {
              let retryReq = addToken(req, authService.getToken());
              if (activeBranch && !req.url.includes('/auth/')) {
                retryReq = retryReq.clone({ headers: retryReq.headers.set('X-Branch-Id', activeBranch.id) });
              }
              return next(retryReq);
            }),
            catchError(refreshError => {
              // Refresh de başarısız → kullanıcıyı login'e yönlendir
              authService.logout();
              router.navigate(['/login']);
              return throwError(() => refreshError);
            })
          );
        }

        authService.logout();
        router.navigate(['/login']);
      }

      return throwError(() => error);
    })
  );
};

function addToken(req: HttpRequest<unknown>, token: string | null): HttpRequest<unknown> {
  if (!token) return req;
  return req.clone({
    headers: req.headers.set('Authorization', `Bearer ${token}`)
  });
}
