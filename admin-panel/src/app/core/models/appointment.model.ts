export interface Appointment {
  id: string;
  customerId: string;
  customerName: string;
  staffId: string;
  staffName: string;
  serviceId: string;
  serviceName: string;
  startTime: string;
  endTime: string;
  status: AppointmentStatus;
  notes?: string;
  durationMinutes: number;
}

export enum AppointmentStatus {
  Pending   = 0,
  Confirmed = 1,
  Completed = 2,
  Cancelled = 3,
  NoShow    = 4
}

export interface AvailableSlot {
  startTime: string;
  endTime: string;
  isAvailable: boolean;
}

export interface CreateAppointmentRequest {
  customerId: string;
  staffId: string;
  serviceId: string;
  startTime: string;
  notes?: string;
}