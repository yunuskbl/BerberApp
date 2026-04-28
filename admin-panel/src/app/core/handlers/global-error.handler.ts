import { ErrorHandler, Injectable, isDevMode } from '@angular/core';

@Injectable()
export class GlobalErrorHandler implements ErrorHandler {
  handleError(error: unknown): void {
    if (isDevMode()) {
      console.error('[GlobalErrorHandler]', error);
    }
    // Production'da hatayı sessizce yut — kullanıcıya sadece UI mesajı göster
  }
}
