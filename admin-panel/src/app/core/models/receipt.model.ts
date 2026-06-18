export interface ReceiptItem {
  id: string;
  serviceName: string;
  quantity: number;
  unitPrice: number;
  lineTotal: number;
}

export interface Receipt {
  id: string;
  receiptNumber: string;
  customerName?: string;
  customerId?: string;
  appointmentId?: string;
  totalAmount: number;
  currency: string;
  note?: string;
  isVoid: boolean;
  createdAt: string;
  items: ReceiptItem[];
}

export interface CreateReceiptItemDto {
  serviceName: string;
  quantity: number;
  unitPrice: number;
}

export interface CreateReceiptDto {
  customerId?: string;
  appointmentId?: string;
  note?: string;
  items: CreateReceiptItemDto[];
}
