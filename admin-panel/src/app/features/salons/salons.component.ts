import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { HttpClient, HttpParams } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { environment } from '../../../environments/environment';

interface Salon {
  id:        string;
  name:      string;
  subdomain: string;
  phone?:    string;
  address?:  string;
  logoUrl?:  string;
}

@Component({
  selector: 'app-salons',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule],
  templateUrl: './salons.component.html',
  styleUrl: './salons.component.scss'
})
export class SalonsComponent implements OnInit {
  salons:      Salon[] = [];
  isLoading    = true;
  searchQuery  = '';
  errorMessage = '';

  constructor(private http: HttpClient) {}

  ngOnInit(): void {
    this.loadSalons();
  }

  loadSalons(): void {
    this.isLoading = true;
    let params = new HttpParams();
    if (this.searchQuery) params = params.set('search', this.searchQuery);

    this.http.get<any>(`${environment.apiUrl}/salons`, { params }).subscribe({
      next: (res) => {
        if (res.success) this.salons = res.data;
        this.isLoading = false;
      },
      error: () => {
        this.errorMessage = 'Salonlar yüklenemedi.';
        this.isLoading    = false;
      }
    });
  }

  onSearch(): void {
    this.loadSalons();
  }

  getInitials(name: string): string {
    return name.split(' ').map(n => n[0]).join('').toUpperCase().slice(0, 2);
  }
}