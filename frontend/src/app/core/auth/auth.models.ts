/** Matches EmployeeRole of the backend. */
export type EmployeeRole = 'Employee' | 'Manager' | 'Hr';

/** Matches CurrentUserResponse of the backend (GET /api/me). */
export interface CurrentUser {
  employeeId: string;
  firstName: string;
  lastName: string;
  email: string;
  role: EmployeeRole;
}

/** Matches AccessTokenResponse of the backend. */
export interface AccessTokenResponse {
  accessToken: string;
  tokenType: string;
  /** Lifetime in seconds. */
  expiresIn: number;
}

/** Matches LoginRequest of the backend. */
export interface LoginCredentials {
  email: string;
  password: string;
  rememberMe: boolean;
}
