export interface Customer {
  id: string;
  fullName: string;
  phone: string;
  email?: string;
  notes?: string;
  /** Tamamlanmış randevu sayısı — sunucuda randevulardan sayılıyor. */
  totalVisits: number;
  /** Son tamamlanmış randevunun tarihi; hiç gelmemişse null. */
  lastVisitAt?: string | null;
}

export interface CreateCustomerRequest {
  fullName: string;
  phone: string;
  email?: string;
  notes?: string;
}

export interface UpdateCustomerRequest {
  fullName: string;
  phone: string;
  email?: string;
  notes?: string;
}