export interface Customer {
  id: string;
  fullName: string;
  phone: string;
  email?: string;
  notes?: string;
  totalVisits: number;
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