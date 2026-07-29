import { createSlice } from '@reduxjs/toolkit';
import { hasSession } from './authStorage';

interface AuthState {
  /** Есть ли активная сессия. Источник истины для шапки и страниц. */
  isAuthenticated: boolean;
}

const initialState: AuthState = {
  isAuthenticated: hasSession(),
};

/**
 * Слайс состояния аутентификации. Токены живут в authStorage (localStorage),
 * слайс хранит только реактивный флаг «вошёл/не вошёл»: компоненты не могут
 * подписаться на localStorage напрямую.
 */
const authSlice = createSlice({
  name: 'auth',
  initialState,
  reducers: {
    /** Сессия открыта (успешный вход). */
    sessionStarted(state) {
      state.isAuthenticated = true;
    },
    /** Сессия закрыта (выход или провал refresh-ротации). */
    sessionEnded(state) {
      state.isAuthenticated = false;
    },
  },
});

export const { sessionStarted, sessionEnded } = authSlice.actions;
export const authReducer = authSlice.reducer;

/** Селектор флага аутентификации. */
export function selectIsAuthenticated(state: { auth: AuthState }): boolean {
  return state.auth.isAuthenticated;
}
