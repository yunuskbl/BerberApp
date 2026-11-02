import { Component } from '@angular/core';
import { AsyncPipe } from '@angular/common';
import { RouterModule } from '@angular/router';
import { Observable } from 'rxjs';
import { CompanyInfo, CompanyInfoService } from '../../../core/services/company-info.service';

@Component({
  selector: 'app-privacy-policy',
  standalone: true,
  imports: [RouterModule, AsyncPipe],
  templateUrl: './privacy-policy.component.html',
  styleUrl: './privacy-policy.component.scss'
})
export class PrivacyPolicyComponent {
  /** Veri sorumlusunun yasal kimliği (sunucudaki COMPANY_* ayarları). */
  readonly company$: Observable<CompanyInfo | null>;

  constructor(companyInfo: CompanyInfoService) {
    this.company$ = companyInfo.info$;
  }
}
