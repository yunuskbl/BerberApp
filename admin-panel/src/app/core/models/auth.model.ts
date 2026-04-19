export interface LoginRequest {
  email: string;
  password: string;
}

export interface LoginResponse {
  accessToken: string;
  refreshToken: string;
  accessTokenExpiry: string;
  email: string;
  fullName: string;
  role: string;
  tenantId: string;
  subdomain?: string;
}