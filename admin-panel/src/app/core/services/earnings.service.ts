import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface EarningsDto {
  totalEarnings:        number;
  totalInTry:           number;
  exchangeRateDate:     string;
  totalAppointments:    number;
  averagePerAppointment: number;
  todayEarnings:        number;
  todayAppointments:    number;
  weekEarnings:         number;
  weekAppointments:     number;
  monthEarnings:        number;
  monthAppointments:    number;
  byCurrency:           CurrencyEarningDto[];
  daily:                DailyEarningDto[];
  byStaff:              StaffEarningDto[];
  byService:            ServiceEarningDto[];
  // P&L
  totalExpenses:        number;
  netProfit:            number;
  expenseByCategory:    ExpenseSummaryDto[];
  /** Güne göre gider, TRY'ye çevrilmiş — net kâr grafiğinin kaynağı. */
  expenseDaily:         DailyExpenseDto[];
  // Fiyat farkı istatistikleri
  totalPriceDifference: number;
  priceDifferenceCount: number;
  priceDifferences:     PriceDifferenceDetailDto[];
}

export interface PriceDifferenceDetailDto {
  appointmentId: string;
  customerName:  string;
  staffName:     string;
  serviceName:   string;
  originalPrice: number;
  actualPrice:   number;
  difference:    number;
  completedAt:   string;
}

export interface ExpenseSummaryDto {
  category: string;
  total:    number;
  count:    number;
}

export interface CurrencyEarningDto {
  currency:        string;
  totalEarnings:   number;
  totalInTry:      number;
  exchangeRate:    number;
  appointmentCount: number;
}

export interface DailyEarningDto {
  date:               string;
  earnings:           number;
  appointmentCount:   number;
}

export interface DailyExpenseDto {
  date:   string;
  total:  number;   // TRY
  count:  number;
}

export interface StaffEarningDto {
  staffId:            string;
  staffName:          string;
  totalEarnings:      number;
  appointmentCount:   number;
  average:            number;
}

export interface ServiceEarningDto {
  serviceId:          string;
  serviceName:        string;
  currency:           string;
  totalEarnings:      number;
  appointmentCount:   number;
  price:              number;
}

// ── Müşteri takibi raporu ───────────────────────────────────────────────────

export interface CustomerTimelinePointDto {
  date:           string;
  newCount:       number;
  returningCount: number;
}

export interface LoyaltyBucketDto {
  label: string;
  count: number;
  order: number;
}

export interface CustomerRankDto {
  id:         string;
  fullName:   string;
  phone:      string;
  visitCount: number;
  totalSpend: number;
  lastVisit:  string | null;
}

export interface AtRiskCustomerDto extends CustomerRankDto {
  daysSinceLastVisit:  number;
  hasMarketingConsent: boolean;
}

export interface CustomerInsightsDto {
  // Seçili dönem
  activeCustomers:         number;
  newCustomers:            number;
  returningCustomers:      number;
  repeatRate:              number;
  averageSpendPerCustomer: number;
  periodRevenue:           number;
  timeline:                CustomerTimelinePointDto[];
  topCustomers:            CustomerRankDto[];
  // Döneme bağlı olmayan kesitler
  totalCustomers:          number;
  everVisitedCustomers:    number;
  averageVisitGapDays:     number;
  loyaltyBuckets:          LoyaltyBucketDto[];
  atRisk:                  AtRiskCustomerDto[];
  atRiskCount:             number;
  atRiskWithConsent:       number;
  atRiskAfterDays:         number;
}

@Injectable({ providedIn: 'root' })
export class EarningsService {
  private apiUrl = `${environment.apiUrl}/earnings`;

  constructor(private http: HttpClient) {}

  getEarnings(
    startDate: string, endDate: string,
    staffId?: string, branchId?: string, consolidated = false
  ): Observable<any> {
    let url = `${this.apiUrl}?startDate=${startDate}&endDate=${endDate}`;
    if (staffId)     url += `&staffId=${staffId}`;
    if (branchId)    url += `&branchId=${branchId}`;
    if (consolidated) url += `&consolidated=true`;
    return this.http.get(url);
  }

  getCustomerInsights(
    startDate: string, endDate: string,
    branchId?: string, consolidated = false, atRiskAfterDays = 60,
  ): Observable<any> {
    let url = `${this.apiUrl}/customers?startDate=${startDate}&endDate=${endDate}`
            + `&atRiskAfterDays=${atRiskAfterDays}`;
    if (branchId)     url += `&branchId=${branchId}`;
    if (consolidated) url += `&consolidated=true`;
    return this.http.get(url);
  }
}