import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';

@Component({
  selector: 'app-payment-result',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './payment-result.component.html',
  styleUrl: './payment-result.component.scss'
})
export class PaymentResultComponent implements OnInit {
  status: 'success' | 'failure' = 'failure';
  plan = '';
  countdown = 5;

  constructor(private route: ActivatedRoute, private router: Router) {}

  ngOnInit() {
    this.status = (this.route.snapshot.queryParamMap.get('status') as any) ?? 'failure';
    this.plan   = this.route.snapshot.queryParamMap.get('plan') ?? '';
    this.startCountdown();
  }

  private startCountdown() {
    const interval = setInterval(() => {
      this.countdown--;
      if (this.countdown <= 0) {
        clearInterval(interval);
        this.router.navigate([this.status === 'success' ? '/dashboard' : '/pricing']);
      }
    }, 1000);
  }

  goNow() {
    this.router.navigate([this.status === 'success' ? '/dashboard' : '/pricing']);
  }
}
