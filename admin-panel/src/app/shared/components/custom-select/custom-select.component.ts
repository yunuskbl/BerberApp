import { Component, Input, Output, EventEmitter, OnInit, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';

export interface SelectOption {
  value: string;
  label: string;
}

@Component({
  selector: 'app-custom-select',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './custom-select.component.html',
  styleUrl: './custom-select.component.scss'
})
export class CustomSelectComponent {
  @Input() options: SelectOption[] = [];
  @Input() placeholder = 'Seçin...';
  @Input() value       = '';
  @Input() hasError    = false;
  @Output() valueChange = new EventEmitter<string>();

  isOpen = false;

  get selectedLabel(): string {
    return this.options.find(o => o.value === this.value)?.label || '';
  }

  toggle(): void {
    this.isOpen = !this.isOpen;
  }

  select(option: SelectOption): void {
    this.value = option.value;
    this.valueChange.emit(option.value);
    this.isOpen = false;
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    const target = event.target as HTMLElement;
    if (!target.closest('app-custom-select')) {
      this.isOpen = false;
    }
  }
}