export type PackageStatus = 'Active' | 'Completed' | 'Cancelled';
export type PackagePaymentMethod = 'Cash' | 'Card' | 'Transfer' | 'Other';

export const PAYMENT_METHOD_LABELS: Record<PackagePaymentMethod, string> = {
  Cash: 'Nakit',
  Card: 'Kart',
  Transfer: 'Havale/EFT',
  Other: 'Diğer',
};

export interface PackageTemplate {
  id: string;
  name: string;
  serviceId?: string | null;
  serviceName?: string | null;
  sessionCount: number;
  price: number;
  currency: string;
  validityDays?: number | null;
  isActive: boolean;
}

export interface PackageTemplateRequest {
  name: string;
  serviceId?: string | null;
  sessionCount: number;
  price: number;
  validityDays?: number | null;
  isActive: boolean;
}

export interface PackageSummary {
  id: string;
  customerId: string;
  customerName: string;
  customerPhone?: string | null;
  name: string;
  serviceId?: string | null;
  serviceName?: string | null;
  totalSessions: number;
  usedSessions: number;
  remainingSessions: number;
  price: number;
  paid: number;
  remainingDebt: number;
  currency: string;
  status: PackageStatus;
  isExpired: boolean;
  startedAt: string;
  expiresAt?: string | null;
  lastSessionAt?: string | null;
  lastActivityAt: string;
  publicToken: string;
  note?: string | null;
}

export interface PackageSession {
  id: string;
  number: number | null;
  performedAt: string;
  staffId?: string | null;
  staffName?: string | null;
  note?: string | null;
  customerConfirmedAt?: string | null;
  isVoid: boolean;
  voidedAt?: string | null;
  voidReason?: string | null;
}

export interface PackagePayment {
  id: string;
  amount: number;
  paidAt: string;
  method: PackagePaymentMethod;
  note?: string | null;
  isVoid: boolean;
  voidedAt?: string | null;
}

/** Seans işlendiğinde müşterinin okutacağı onay kodu. */
export interface SessionConfirm {
  sessionId: string;
  code: string;
  expiresAt: string;
}

export interface PackageDetail {
  package: PackageSummary;
  sessions: PackageSession[];
  payments: PackagePayment[];
  cancelledAt?: string | null;
  cancelReason?: string | null;
  confirm?: SessionConfirm | null;
}

export interface SellPackageRequest {
  customerId: string;
  templateId?: string | null;
  name?: string | null;
  serviceId?: string | null;
  totalSessions?: number | null;
  price?: number | null;
  validityDays?: number | null;
  note?: string | null;
  initialPayment?: { amount: number; method: PackagePaymentMethod } | null;
}

// ── Müşteri tarafı (giriş istemez) ─────────────────────────────────────────

export interface PublicSalon {
  name: string;
  subdomain: string;
  logoUrl?: string | null;
  phone?: string | null;
  address?: string | null;
}

export interface PublicPackage {
  salon: PublicSalon;
  customerFirstName: string;
  package: {
    name: string;
    serviceName?: string | null;
    totalSessions: number;
    usedSessions: number;
    remainingSessions: number;
    status: PackageStatus;
    isExpired: boolean;
    startedAt: string;
    expiresAt?: string | null;
    price: number;
    paid: number;
    remainingDebt: number;
    currency: string;
  };
  sessions: { number: number | null; performedAt: string; staffName: string; confirmed: boolean; isVoid: boolean }[];
  payments: { amount: number; paidAt: string; method: PackagePaymentMethod }[];
}

export interface PublicSessionInfo {
  salon: PublicSalon;
  packageName: string;
  sessionNumber: number | null;
  totalSessions: number;
  performedAt: string;
  staffName: string;
  confirmed: boolean;
  confirmedAt?: string | null;
  isVoid: boolean;
  expired: boolean;
  packageToken: string;
}

/** Müşteri bağlantıları panelin kendi adresinden üretilir (aynı uygulama). */
export function packageTrackUrl(token: string): string {
  return `${window.location.origin}/paket/${token}`;
}

export function sessionConfirmUrl(code: string): string {
  return `${window.location.origin}/seans-onay/${code}`;
}
