import { createApi, fetchBaseQuery } from '@reduxjs/toolkit/query/react';

/** Ключ access-токена в localStorage. Пишет слой аутентификации; здесь только чтение. */
export const ACCESS_TOKEN_STORAGE_KEY = 'gp.accessToken';

/**
 * Базовый слой обращений к HTTP API (ADR-0018 §3 п. 5): единственная точка,
 * через которую интерфейс общается с backend. Конкретные эндпоинты
 * подключаются через injectEndpoints в своих модулях (features).
 *
 * Базовый адрес берётся из конфигурации (VITE_API_URL), не из кода:
 * в dev это «/api» через прокси Vite, в проде — тот же origin.
 *
 * Authorization: если в localStorage лежит access-токен — подставляется
 * Bearer-заголовок. Полный слой аутентификации (вход, refresh, выход)
 * добавится со страницами входа/регистрации; до него токен появляется
 * в хранилище только вручную (отладка гейтов).
 */
export const baseApi = createApi({
  reducerPath: 'api',
  baseQuery: fetchBaseQuery({
    baseUrl: import.meta.env.VITE_API_URL ?? '/api',
    prepareHeaders: (headers) => {
      const token = localStorage.getItem(ACCESS_TOKEN_STORAGE_KEY);
      if (token) {
        headers.set('Authorization', `Bearer ${token}`);
      }
      return headers;
    },
  }),
  tagTypes: ['Dishes', 'Categories', 'Tags', 'Profile', 'Subscription', 'Plans'],
  endpoints: () => ({}),
});
