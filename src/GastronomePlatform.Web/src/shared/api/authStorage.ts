/**
 * Хранилище сессии аутентификации. Решение Этапа 4: оба токена в localStorage —
 * сессия переживает перезагрузку, слой минимален; XSS-риск принят осознанно
 * (смягчение — access живёт 15 минут, refresh ротируется при каждом обновлении).
 * Переход на httpOnly-cookie — отложенная задача безопасности будущих этапов.
 */

export const ACCESS_TOKEN_STORAGE_KEY = 'gp.accessToken';
export const REFRESH_TOKEN_STORAGE_KEY = 'gp.refreshToken';

/** Пара токенов, сохраняемая после входа или refresh-ротации. */
export interface StoredSession {
  accessToken: string;
  refreshToken: string;
}

/** Сохраняет пару токенов (вход или ротация refresh). */
export function saveSession(session: StoredSession): void {
  localStorage.setItem(ACCESS_TOKEN_STORAGE_KEY, session.accessToken);
  localStorage.setItem(REFRESH_TOKEN_STORAGE_KEY, session.refreshToken);
}

/** Текущий access-токен или null, если сессии нет. */
export function getAccessToken(): string | null {
  return localStorage.getItem(ACCESS_TOKEN_STORAGE_KEY);
}

/** Текущий refresh-токен или null, если сессии нет. */
export function getRefreshToken(): string | null {
  return localStorage.getItem(REFRESH_TOKEN_STORAGE_KEY);
}

/** Полностью очищает сессию (выход, провал refresh). */
export function clearSession(): void {
  localStorage.removeItem(ACCESS_TOKEN_STORAGE_KEY);
  localStorage.removeItem(REFRESH_TOKEN_STORAGE_KEY);
}

/** Есть ли сохранённая сессия (для начального состояния authSlice). */
export function hasSession(): boolean {
  return getAccessToken() !== null;
}
