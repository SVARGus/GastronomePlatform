import { baseApi } from '../../shared/api/baseApi';
import { sessionStarted } from '../../shared/api/authSlice';
import { saveSession } from '../../shared/api/authStorage';
import type { LoginRequest, LoginResponse, RegisterRequest } from '../../shared/api/types/auth';

/** Эндпоинты модуля Auth: вход, регистрация, выход. Refresh живёт в baseApi (reauth). */
export const authApi = baseApi.injectEndpoints({
  endpoints: (build) => ({
    /** Вход: сохраняет пару токенов и поднимает флаг сессии централизованно. */
    login: build.mutation<LoginResponse, LoginRequest>({
      query: (body) => ({ url: 'auth/login', method: 'POST', body }),
      async onQueryStarted(_arg, { dispatch, queryFulfilled }) {
        const { data } = await queryFulfilled;
        saveSession({ accessToken: data.accessToken, refreshToken: data.refreshToken });
        dispatch(sessionStarted());
      },
    }),
    /** Регистрация: 204 без токенов — вход отдельным шагом. */
    register: build.mutation<void, RegisterRequest>({
      query: (body) => ({ url: 'auth/register', method: 'POST', body }),
    }),
    /** Выход: отзывает refresh-токен. Локальную очистку сессии делает вызывающий. */
    logout: build.mutation<void, string>({
      query: (refreshToken) => ({ url: 'auth/logout', method: 'POST', body: { refreshToken } }),
    }),
  }),
});

export const { useLoginMutation, useRegisterMutation, useLogoutMutation } = authApi;
