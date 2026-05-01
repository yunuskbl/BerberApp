import { Pipe, PipeTransform } from '@angular/core';
import { LanguageService } from '../../core/services/language.service';

@Pipe({
  name: 'translate',
  standalone: true,
  pure: false,   // re-evaluates when language changes
})
export class TranslatePipe implements PipeTransform {
  constructor(private lang: LanguageService) {}

  transform(key: string): string {
    return this.lang.t(key);
  }
}
