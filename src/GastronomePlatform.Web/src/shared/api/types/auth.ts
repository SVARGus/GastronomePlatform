/**
 * Типы контракта модуля Auth (ручное повторение DTO backend, как в dishes.ts).
 */

/** Тело POST /api/auth/login. Login — email, никнейм или телефон. */
export interface LoginRequest {
  login: string;
  password: string;
}

/** Тело POST /api/auth/register. Ответ — 204 без токенов. */
export interface RegisterRequest {
  email: string;
  userName: string;
  password: string;
  phone: string | null;
}

/** Ответ login/refresh: пара токенов + момент истечения access (UTC). */
export interface LoginResponse {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
}
