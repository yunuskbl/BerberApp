export interface Staff {
  id: string;
  fullName: string;
  phone?: string;
  avatarUrl?: string;
  bio?: string;
  isActive: boolean;
}

export interface CreateStaffRequest {
  fullName: string;
  phone?: string;
  avatarUrl?: string;
  bio?: string;
}

export interface UpdateStaffRequest {
  fullName: string;
  phone?: string;
  avatarUrl?: string;
  bio?: string;
  isActive: boolean;
}