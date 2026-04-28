import { HttpInterceptorFn, HttpRequest, HttpHandlerFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, switchMap, throwError } from 'rxjs';
import { Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  const cloned = addToken(req, authService.getToken());

  return next(cloned).pipe(
    catchError((error: HttpErrorResponse) => {
      // Refresh endpoint'inin kendisi 401 dönerse sonsuz döngüye girme
      if (error.status === 401 && !req.url.includes('/auth/refresh') && !req.url.includes('/auth/login')) {
        const refreshToken = authService.getRefreshToken();

        if (refreshToken) {
          return authService.refreshToken().pipe(
            switchMap(() => {
              // Yeni access token ile orijinal isteği tekrar gönder
              return next(addToken(req, authService.getToken()));
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
