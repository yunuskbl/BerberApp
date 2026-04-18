export interface Service {
  id: string;
  name: string;
  durationMinutes: number;
  price: number;
  currency: string;
  color?: string;
  isActive: boolean;
}

export interface CreateServiceRequest {
  name: string;
  durationMinutes: number;
  price: number;
  currency: string;
  color?: string;
}

export interface UpdateServiceRequest {
  name: string;
  durationMinutes: number;
  price: number;
  currency: string;
  color?: string;
  isActive: boolean;
}