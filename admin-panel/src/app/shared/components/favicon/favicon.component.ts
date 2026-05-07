import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-favicon',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './favicon.component.html',
  styleUrl: './favicon.component.scss'
})
export class FaviconComponent {
  @Input() size: 32 | 16 = 32;
}
