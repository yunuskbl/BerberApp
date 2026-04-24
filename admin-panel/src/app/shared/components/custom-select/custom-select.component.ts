import { Component, Input, Output, EventEmitter, HostListener, ElementRef, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

export interface SelectOption {
  value: string;
  label: string;
}

@Component({
  selector: 'app-custom-select',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './custom-select.component.html',
  styleUrl: './custom-select.component.scss'
})
export class CustomSelectComponent {
  @Input() options: SelectOption[] = [];
  @Input() placeholder = 'Seçin...';
  @Input() value       = '';
  @Input() hasError    = false;
  @Input() searchable  = false;
  @Output() valueChange = new EventEmitter<string>();

  @ViewChild('searchInput') searchInput?: ElementRef<HTMLInputElement>;

  isOpen    = false;
  searchText = '';

  get selectedLabel(): string {
    return this.options.find(o => o.value === this.value)?.label || '';
  }

  get filteredOptions(): SelectOption[] {
    if (!this.searchable || !this.searchText.trim()) return this.options;
    const q = this.searchText.toLowerCase();
    return this.options.filter(o => o.label.toLowerCase().includes(q));
  }

  toggle(): void {
    this.isOpen = !this.isOpen;
    if (this.isOpen && this.searchable) {
      this.searchText = '';
      setTimeout(() => this.searchInput?.nativeElement.focus(), 50);
    }
  }

  select(option: SelectOption): void {
    this.value = option.value;
    this.valueChange.emit(option.value);
    this.isOpen    = false;
    this.searchText = '';
  }

  clearSelection(event: MouseEvent): void {
    event.stopPropagation();
    this.value = '';
    this.valueChange.emit('');
    this.searchText = '';
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    const target = event.target as HTMLElement;
    if (!target.closest('app-custom-select')) {
      this.isOpen    = false;
      this.searchText = '';
    }
  }
}